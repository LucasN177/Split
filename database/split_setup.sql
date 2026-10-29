-- =====================================================================
-- Split – Datenbank-Setup für Supabase
--
-- Ausführen: Supabase Dashboard → SQL Editor → Inhalt einfügen → Run
--
-- * Idempotent: kann beliebig oft ausgeführt werden.
-- * Alle Objekte tragen das Präfix "split_", damit sich die App die
--   Datenbank gefahrlos mit anderen Projekten teilen kann.
-- * Die gemeinsam genutzte Tabelle public.users wird NUR gelesen
--   (Profil-Anzeige, Suche per Benutzername) – keine Änderungen an
--   Struktur, Triggern oder Policies.
-- * Eine bereits vorhandene Tabelle public.split_groups wird nur um
--   fehlende Spalten ergänzt, bestehende Daten bleiben erhalten.
-- =====================================================================

create extension if not exists "pgcrypto";

-- ---------------------------------------------------------------------
-- 1. Tabellen
-- ---------------------------------------------------------------------

create table if not exists public.split_groups (
    id          uuid primary key default gen_random_uuid(),
    name        text not null,
    description text default '',
    created_at  timestamptz not null default now()
);

-- Spalten ergänzen, falls die Tabelle schon in einer älteren Form existiert
alter table public.split_groups add column if not exists description text default '';
alter table public.split_groups add column if not exists currency    text not null default 'EUR';
alter table public.split_groups add column if not exists created_by  uuid default auth.uid() references auth.users (id) on delete cascade;
alter table public.split_groups add column if not exists created_at  timestamptz not null default now();

-- Mitglieder: registrierte Benutzer (user_id) oder reine Namen (user_id = null)
create table if not exists public.split_group_members (
    id           uuid primary key default gen_random_uuid(),
    group_id     uuid not null references public.split_groups (id) on delete cascade,
    user_id      uuid null references auth.users (id) on delete set null,
    display_name text not null,
    created_at   timestamptz not null default now(),
    unique (group_id, user_id)
);

create index if not exists split_group_members_group_idx on public.split_group_members (group_id);
create index if not exists split_group_members_user_idx  on public.split_group_members (user_id);

create table if not exists public.split_expenses (
    id           uuid primary key default gen_random_uuid(),
    group_id     uuid not null references public.split_groups (id) on delete cascade,
    title        text not null,
    amount       numeric(12, 2) not null check (amount > 0),
    paid_by      uuid not null references public.split_group_members (id) on delete cascade,
    category     text not null default 'Sonstiges',
    expense_date date not null default current_date,
    created_by   uuid default auth.uid() references auth.users (id) on delete set null,
    created_at   timestamptz not null default now()
);

create index if not exists split_expenses_group_idx on public.split_expenses (group_id);

create table if not exists public.split_expense_shares (
    id         uuid primary key default gen_random_uuid(),
    expense_id uuid not null references public.split_expenses (id) on delete cascade,
    member_id  uuid not null references public.split_group_members (id) on delete cascade,
    amount     numeric(12, 2) not null check (amount >= 0)
);

create index if not exists split_expense_shares_expense_idx on public.split_expense_shares (expense_id);

create table if not exists public.split_settlements (
    id          uuid primary key default gen_random_uuid(),
    group_id    uuid not null references public.split_groups (id) on delete cascade,
    from_member uuid not null references public.split_group_members (id) on delete cascade,
    to_member   uuid not null references public.split_group_members (id) on delete cascade,
    amount      numeric(12, 2) not null check (amount > 0),
    note        text not null default '',
    settled_at  date not null default current_date,
    created_by  uuid default auth.uid() references auth.users (id) on delete set null,
    created_at  timestamptz not null default now()
);

create index if not exists split_settlements_group_idx on public.split_settlements (group_id);

-- ---------------------------------------------------------------------
-- 2. Funktionen & Trigger
-- ---------------------------------------------------------------------

-- Anzeigename eines Benutzers aus public.users (Vorname Nachname → Benutzername → E-Mail-Präfix)
create or replace function public.split_display_name(uid uuid)
returns text
language sql
stable
security definer set search_path = public
as $$
    select coalesce(
        (select coalesce(
                    nullif(trim(coalesce(u.vorname, '') || ' ' || coalesce(u.nachname, '')), ''),
                    nullif(u.username, ''))
         from public.users u where u.id = uid),
        (select nullif(split_part(coalesce(a.email, ''), '@', 1), '') from auth.users a where a.id = uid),
        'Ich'
    );
$$;

-- Ist der angemeldete Benutzer Mitglied (oder Ersteller) der Gruppe?
-- security definer verhindert eine Rekursion in den RLS-Policies.
create or replace function public.split_is_group_member(gid uuid)
returns boolean
language sql
stable
security definer set search_path = public
as $$
    select exists (
        select 1 from public.split_group_members
        where group_id = gid and user_id = auth.uid()
    ) or exists (
        select 1 from public.split_groups
        where id = gid and created_by = auth.uid()
    );
$$;

-- Ersteller wird automatisch Mitglied seiner neuen Gruppe
create or replace function public.split_handle_new_group()
returns trigger
language plpgsql
security definer set search_path = public
as $$
begin
    if new.created_by is not null then
        insert into public.split_group_members (group_id, user_id, display_name)
        values (new.id, new.created_by, public.split_display_name(new.created_by))
        on conflict (group_id, user_id) do nothing;
    end if;
    return new;
end;
$$;

drop trigger if exists split_on_group_created on public.split_groups;
create trigger split_on_group_created
    after insert on public.split_groups
    for each row execute function public.split_handle_new_group();

-- Benutzer per Benutzername finden (zum Hinzufügen in Gruppen),
-- ohne die Policies der gemeinsam genutzten Tabelle users zu ändern
create or replace function public.split_find_user(p_username text)
returns table (id uuid, username text, vorname text, nachname text)
language sql
stable
security definer set search_path = public
as $$
    select u.id, coalesce(u.username, ''), coalesce(u.vorname, ''), coalesce(u.nachname, '')
    from public.users u
    where auth.uid() is not null
      and lower(u.username) = lower(trim(p_username))
    limit 1;
$$;

revoke all on function public.split_find_user(text) from public, anon;
grant execute on function public.split_find_user(text) to authenticated;

-- ---------------------------------------------------------------------
-- 3. Row Level Security
-- ---------------------------------------------------------------------

alter table public.split_groups         enable row level security;
alter table public.split_group_members  enable row level security;
alter table public.split_expenses       enable row level security;
alter table public.split_expense_shares enable row level security;
alter table public.split_settlements    enable row level security;

drop policy if exists "split_groups_select" on public.split_groups;
create policy "split_groups_select" on public.split_groups
    for select to authenticated using (public.split_is_group_member(id));

drop policy if exists "split_groups_insert" on public.split_groups;
create policy "split_groups_insert" on public.split_groups
    for insert to authenticated with check (created_by = auth.uid());

drop policy if exists "split_groups_update" on public.split_groups;
create policy "split_groups_update" on public.split_groups
    for update to authenticated using (public.split_is_group_member(id));

drop policy if exists "split_groups_delete" on public.split_groups;
create policy "split_groups_delete" on public.split_groups
    for delete to authenticated using (created_by = auth.uid());

drop policy if exists "split_group_members_all" on public.split_group_members;
create policy "split_group_members_all" on public.split_group_members
    for all to authenticated
    using (public.split_is_group_member(group_id))
    with check (public.split_is_group_member(group_id));

drop policy if exists "split_expenses_all" on public.split_expenses;
create policy "split_expenses_all" on public.split_expenses
    for all to authenticated
    using (public.split_is_group_member(group_id))
    with check (public.split_is_group_member(group_id));

drop policy if exists "split_expense_shares_all" on public.split_expense_shares;
create policy "split_expense_shares_all" on public.split_expense_shares
    for all to authenticated
    using (exists (select 1 from public.split_expenses e
                   where e.id = expense_id and public.split_is_group_member(e.group_id)))
    with check (exists (select 1 from public.split_expenses e
                        where e.id = expense_id and public.split_is_group_member(e.group_id)));

drop policy if exists "split_settlements_all" on public.split_settlements;
create policy "split_settlements_all" on public.split_settlements
    for all to authenticated
    using (public.split_is_group_member(group_id))
    with check (public.split_is_group_member(group_id));

-- ---------------------------------------------------------------------
-- 4. Rechte für die API-Rollen
-- ---------------------------------------------------------------------

grant select, insert, update, delete on
    public.split_groups,
    public.split_group_members,
    public.split_expenses,
    public.split_expense_shares,
    public.split_settlements
to authenticated;

-- PostgREST-Schema-Cache neu laden, damit die API die Tabellen sofort kennt
notify pgrst, 'reload schema';
