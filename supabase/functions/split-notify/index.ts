// Supabase Edge Function "split-notify"
//
// Sendet Web-Push-Benachrichtigungen an alle Mitglieder einer Gruppe (außer dem Ersteller),
// wenn eine neue Ausgabe oder Zahlung angelegt wird.
//
// Auslöser: Database Webhooks im Supabase Dashboard (Database → Webhooks) für
//   INSERT auf public.split_expenses und public.split_settlements
//   → Typ "Supabase Edge Functions" → split-notify
//
// Benötigte Secrets (supabase secrets set …):
//   VAPID_PUBLIC_KEY, VAPID_PRIVATE_KEY, VAPID_SUBJECT (z.B. mailto:du@example.com)
//   APP_URL (z.B. https://<user>.github.io/Split/)
// SUPABASE_URL und SUPABASE_SERVICE_ROLE_KEY stellt Supabase automatisch bereit.

import { createClient } from "jsr:@supabase/supabase-js@2";
import webpush from "npm:web-push@3.6.7";

type WebhookPayload = {
  type: "INSERT" | "UPDATE" | "DELETE";
  table: string;
  record: Record<string, unknown>;
};

const supabase = createClient(
  Deno.env.get("SUPABASE_URL")!,
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
);

webpush.setVapidDetails(
  Deno.env.get("VAPID_SUBJECT") ?? "mailto:admin@example.com",
  Deno.env.get("VAPID_PUBLIC_KEY")!,
  Deno.env.get("VAPID_PRIVATE_KEY")!,
);

const appUrl = (Deno.env.get("APP_URL") ?? "./").replace(/\/?$/, "/");

const money = (amount: unknown, currency: string) =>
  new Intl.NumberFormat("de-DE", { style: "currency", currency }).format(Number(amount));

Deno.serve(async (req) => {
  const payload = (await req.json()) as WebhookPayload;
  if (payload.type !== "INSERT") {
    return new Response("ignoriert", { status: 200 });
  }

  const record = payload.record;
  const groupId = record.group_id as string;
  const createdBy = record.created_by as string | null;

  const { data: group } = await supabase
    .from("split_groups")
    .select("name, currency")
    .eq("id", groupId)
    .single();
  if (!group) return new Response("Gruppe nicht gefunden", { status: 200 });

  const { data: members } = await supabase
    .from("split_group_members")
    .select("id, user_id, display_name")
    .eq("group_id", groupId);

  const nameOf = (memberId: unknown) =>
    members?.find((m) => m.id === memberId)?.display_name ?? "Jemand";

  // Wiederkehrende Ausgaben haben keinen menschlichen Ersteller → alle benachrichtigen
  const recipients = (members ?? [])
    .map((m) => m.user_id as string | null)
    .filter((id): id is string => !!id && id !== createdBy);
  if (recipients.length === 0) return new Response("keine Empfänger", { status: 200 });

  const notification = payload.table === "split_settlements"
    ? {
      title: group.name,
      body: `${nameOf(record.from_member)} hat ${money(record.amount, group.currency)} an ${nameOf(record.to_member)} gezahlt`,
    }
    : {
      title: group.name,
      body: `${nameOf(record.paid_by)}: ${record.title} – ${money(record.amount, group.currency)}`,
    };

  const { data: subscriptions } = await supabase
    .from("split_push_subscriptions")
    .select("endpoint, p256dh, auth")
    .in("user_id", recipients);

  const message = JSON.stringify({
    ...notification,
    url: `${appUrl}groups/${groupId}`,
    tag: `split-${groupId}`,
  });

  const results = await Promise.allSettled((subscriptions ?? []).map(async (s) => {
    try {
      await webpush.sendNotification(
        { endpoint: s.endpoint, keys: { p256dh: s.p256dh, auth: s.auth } },
        message,
      );
    } catch (error) {
      // Abgelaufene Abos (404/410) aufräumen
      const status = (error as { statusCode?: number }).statusCode;
      if (status === 404 || status === 410) {
        await supabase.from("split_push_subscriptions").delete().eq("endpoint", s.endpoint);
      }
      throw error;
    }
  }));

  const sent = results.filter((r) => r.status === "fulfilled").length;
  return new Response(JSON.stringify({ sent, total: results.length }), {
    headers: { "Content-Type": "application/json" },
  });
});
