-- =====================================================================
-- Split – Datenbankschema für Supabase
-- Idempotent: kann mehrfach im Supabase SQL Editor ausgeführt werden.
-- =====================================================================

create extension if not exists "pgcrypto";

-- ---------------------------------------------------------------------
-- Benutzerprofile (1:1 zu auth.users)
-- ---------------------------------------------------------------------
create table if not exists public.users (
    id        uuid primary key references auth.users (id) on delete cascade,
    username  text not null default '',
    vorname   text not null default '',
    nachname  text not null default ''
);

alter table public.users enable row level security;

drop policy if exists "users_select_authenticated" on public.users;
create policy "users_select_authenticated" on public.users
    for select to authenticated using (true);

drop policy if exists "users_insert_self" on public.users;
create policy "users_insert_self" on public.users
    for insert to authenticated with check (id = auth.uid());

drop policy if exists "users_update_self" on public.users;
create policy "users_update_self" on public.users
    for update to authenticated using (id = auth.uid()) with check (id = auth.uid());

-- Legt beim Registrieren automatisch ein Profil an
create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer set search_path = public
as $$
begin
    insert into public.users (id, username, vorname, nachname)
    values (
        new.id,
        coalesce(nullif(new.raw_user_meta_data ->> 'username', ''), split_part(coalesce(new.email, ''), '@', 1)),
        coalesce(new.raw_user_meta_data ->> 'vorname', ''),
        coalesce(new.raw_user_meta_data ->> 'nachname', '')
    )
    on conflict (id) do nothing;
    return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
    after insert on auth.users
    for each row execute function public.handle_new_user();

-- ---------------------------------------------------------------------
-- Gruppen & Mitglieder
-- ---------------------------------------------------------------------
create table if not exists public.groups (
    id          uuid primary key default gen_random_uuid(),
    name        text not null,
    description text not null default '',
    currency    text not null default 'EUR',
    created_by  uuid not null default auth.uid() references public.users (id) on delete cascade,
    created_at  timestamptz not null default now()
);

-- Mitglieder können registrierte Benutzer (user_id) oder reine Namen sein
create table if not exists public.group_members (
    id           uuid primary key default gen_random_uuid(),
    group_id     uuid not null references public.groups (id) on delete cascade,
    user_id      uuid null references public.users (id) on delete set null,
    display_name text not null,
    created_at   timestamptz not null default now(),
    unique (group_id, user_id)
);

create index if not exists group_members_group_idx on public.group_members (group_id);
create index if not exists group_members_user_idx on public.group_members (user_id);

-- Hilfsfunktion für RLS (security definer verhindert Rekursion in Policies)
create or replace function public.is_group_member(gid uuid)
returns boolean
language sql
stable
security definer set search_path = public
as $$
    select exists (
        select 1 from public.group_members
        where group_id = gid and user_id = auth.uid()
    ) or exists (
        select 1 from public.groups
        where id = gid and created_by = auth.uid()
    );
$$;

-- Ersteller wird automatisch Mitglied seiner Gruppe
create or replace function public.handle_new_group()
returns trigger
language plpgsql
security definer set search_path = public
as $$
declare
    creator_name text;
begin
    select coalesce(nullif(trim(vorname || ' ' || nachname), ''), nullif(username, ''), 'Ich')
    into creator_name
    from public.users where id = new.created_by;

    insert into public.group_members (group_id, user_id, display_name)
    values (new.id, new.created_by, coalesce(creator_name, 'Ich'))
    on conflict (group_id, user_id) do nothing;
    return new;
end;
$$;

drop trigger if exists on_group_created on public.groups;
create trigger on_group_created
    after insert on public.groups
    for each row execute function public.handle_new_group();

alter table public.groups enable row level security;
alter table public.group_members enable row level security;

drop policy if exists "groups_select" on public.groups;
create policy "groups_select" on public.groups
    for select to authenticated using (public.is_group_member(id));

drop policy if exists "groups_insert" on public.groups;
create policy "groups_insert" on public.groups
    for insert to authenticated with check (created_by = auth.uid());

drop policy if exists "groups_update" on public.groups;
create policy "groups_update" on public.groups
    for update to authenticated using (public.is_group_member(id));

drop policy if exists "groups_delete" on public.groups;
create policy "groups_delete" on public.groups
    for delete to authenticated using (created_by = auth.uid());

drop policy if exists "group_members_all" on public.group_members;
create policy "group_members_all" on public.group_members
    for all to authenticated
    using (public.is_group_member(group_id))
    with check (public.is_group_member(group_id));

-- ---------------------------------------------------------------------
-- Ausgaben
-- ---------------------------------------------------------------------
create table if not exists public.expenses (
    id           uuid primary key default gen_random_uuid(),
    group_id     uuid not null references public.groups (id) on delete cascade,
    title        text not null,
    amount       numeric(12, 2) not null check (amount > 0),
    paid_by      uuid not null references public.group_members (id) on delete cascade,
    category     text not null default 'Sonstiges',
    expense_date date not null default current_date,
    created_by   uuid not null default auth.uid() references public.users (id) on delete set null,
    created_at   timestamptz not null default now()
);

create index if not exists expenses_group_idx on public.expenses (group_id);

create table if not exists public.expense_shares (
    id         uuid primary key default gen_random_uuid(),
    expense_id uuid not null references public.expenses (id) on delete cascade,
    member_id  uuid not null references public.group_members (id) on delete cascade,
    amount     numeric(12, 2) not null check (amount >= 0)
);

create index if not exists expense_shares_expense_idx on public.expense_shares (expense_id);

-- ---------------------------------------------------------------------
-- Ausgleichszahlungen
-- ---------------------------------------------------------------------
create table if not exists public.settlements (
    id          uuid primary key default gen_random_uuid(),
    group_id    uuid not null references public.groups (id) on delete cascade,
    from_member uuid not null references public.group_members (id) on delete cascade,
    to_member   uuid not null references public.group_members (id) on delete cascade,
    amount      numeric(12, 2) not null check (amount > 0),
    note        text not null default '',
    settled_at  date not null default current_date,
    created_by  uuid not null default auth.uid() references public.users (id) on delete set null,
    created_at  timestamptz not null default now()
);

create index if not exists settlements_group_idx on public.settlements (group_id);

alter table public.expenses enable row level security;
alter table public.expense_shares enable row level security;
alter table public.settlements enable row level security;

drop policy if exists "expenses_all" on public.expenses;
create policy "expenses_all" on public.expenses
    for all to authenticated
    using (public.is_group_member(group_id))
    with check (public.is_group_member(group_id));

drop policy if exists "expense_shares_all" on public.expense_shares;
create policy "expense_shares_all" on public.expense_shares
    for all to authenticated
    using (exists (select 1 from public.expenses e where e.id = expense_id and public.is_group_member(e.group_id)))
    with check (exists (select 1 from public.expenses e where e.id = expense_id and public.is_group_member(e.group_id)));

drop policy if exists "settlements_all" on public.settlements;
create policy "settlements_all" on public.settlements
    for all to authenticated
    using (public.is_group_member(group_id))
    with check (public.is_group_member(group_id));
