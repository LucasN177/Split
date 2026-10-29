# Push-Benachrichtigungen einrichten

Split benachrichtigt Gruppenmitglieder, wenn jemand eine Ausgabe oder Zahlung anlegt. Dafür werden einmalig
VAPID-Schlüssel, eine Supabase Edge Function und zwei Database Webhooks benötigt. Ohne diese Einrichtung
funktioniert die App normal – der Schalter in den Einstellungen wird dann einfach nicht angezeigt.

## 1. VAPID-Schlüssel erzeugen

```bash
npx web-push generate-vapid-keys
```

Ergibt einen öffentlichen und einen privaten Schlüssel.

## 2. Öffentlichen Schlüssel an die App geben

- **GitHub:** Repository → Settings → Secrets and variables → Actions → neues Secret `VAPID_PUBLIC_KEY`.
  Der Deploy-Workflow schreibt ihn beim Build in `wwwroot/appsettings.json`.
- **Lokal:** in `wwwroot/appsettings.Development.json` ergänzen:

```json
"Push": { "VapidPublicKey": "<öffentlicher Schlüssel>" }
```

## 3. Edge Function deployen

Mit der [Supabase CLI](https://supabase.com/docs/guides/cli):

```bash
supabase login
supabase link --project-ref <projekt-ref>
supabase secrets set VAPID_PUBLIC_KEY=<öffentlich> VAPID_PRIVATE_KEY=<privat> VAPID_SUBJECT=mailto:<deine-mail> APP_URL=https://<user>.github.io/Split/
supabase functions deploy split-notify --no-verify-jwt
```

Der Code liegt in `supabase/functions/split-notify/index.ts`.

## 4. Database Webhooks anlegen

Supabase Dashboard → Database → Webhooks → *Create a new hook* (zweimal):

| Name                  | Tabelle             | Events | Typ                    | Funktion       |
|-----------------------|---------------------|--------|------------------------|----------------|
| split_notify_expense  | `split_expenses`    | Insert | Supabase Edge Function | `split-notify` |
| split_notify_payment  | `split_settlements` | Insert | Supabase Edge Function | `split-notify` |

## 5. Aktivieren

In der App unter **Einstellungen → Benachrichtigungen** den Schalter einschalten. Auf iOS funktioniert Web-Push
nur, wenn Split über „Zum Home-Bildschirm“ installiert wurde (ab iOS 16.4).
