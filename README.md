# Split

Split ist eine moderne Progressive Web App (PWA) zur einfachen Verwaltung gemeinsamer Ausgaben mit Freunden, Familie oder Gruppen. Die Anwendung hilft dabei, Kosten transparent zu erfassen, offene Beträge zu berechnen und Ausgleichszahlungen übersichtlich nachzuverfolgen.

Die Anwendung basiert auf Blazor WebAssembly als Frontend-Technologie und nutzt Supabase als Backend für Authentifizierung, Datenhaltung und Echtzeitfunktionen.

⸻

Features

* 💰 Gemeinsame Ausgaben erfassen – gleichmäßig, nach Beträgen, Prozent oder Anteilen aufteilen
* 🧾 Notizen und Belegfotos zu Ausgaben
* 💱 Ausgaben in Fremdwährung mit automatischem EZB-Wechselkurs
* 🔁 Wiederkehrende Ausgaben (wöchentlich, monatlich, jährlich)
* 👥 Gruppen mit Einladungslink, Admin-Rolle, Gruppe verlassen, Namens-Mitglieder später mit Konto verknüpfen
* 📊 Automatische Salden, minimale Ausgleichsvorschläge und Statistiken (Kategorien, Mitglieder, Verlauf)
* 🔄 Ausgleichszahlungen erfassen und bearbeiten
* 🔎 Ausgaben durchsuchen und nach Kategorie oder Mitglied filtern
* ⚡ Live-Aktualisierung über Supabase Realtime
* 🔔 Push-Benachrichtigungen bei neuen Ausgaben (optional, siehe docs/push-notifications.md)
* 📴 Offline-Modus: zuletzt geladene Daten lesen, neue Ausgaben offline erfassen und später synchronisieren
* 📱 Installierbare PWA mit Update-Hinweis
* 🔐 Anmeldung, Registrierung, Passwort-Reset und -Änderung über Supabase Auth
* 📤 CSV-Export je Gruppe

⸻

Technologie-Stack

Frontend

* Blazor WebAssembly (.NET)
* Progressive Web App (PWA)
* HTML5 / CSS3
* JavaScript Interop

Backend

* Supabase
    * Authentication
    * PostgreSQL Database
    * Realtime Services
    * Storage

Infrastruktur

* .NET
* REST APIs
* PostgreSQL

⸻

Anwendungsfall

Split eignet sich ideal für:

* Urlaubsreisen
* Wohngemeinschaften
* Gemeinsame Restaurantbesuche
* Veranstaltungen
* Vereins- und Freizeitgruppen
* Alle Situationen, in denen mehrere Personen gemeinsame Kosten teilen

Beispiel:

Person	Bezahlt
Anna	60 €
Ben	30 €
Chris	0 €

Gesamtausgaben: 90 €
Anteil pro Person: 30 €

Ergebnis:

* Ben schuldet Anna 0 €
* Chris schuldet Anna 30 €

Split berechnet diese Verhältnisse automatisch und stellt die notwendigen Ausgleichszahlungen übersichtlich dar.

⸻

Lokale Entwicklung

Voraussetzungen

* .NET SDK 10.0 oder höher
* Supabase-Projekt
* Git

Repository klonen

git clone https://github.com/<username>/split.git
cd split

Datenbank einrichten

Führe das Skript database/split_setup.sql einmalig im SQL Editor deines Supabase-Projekts aus (Dashboard → SQL Editor → Inhalt einfügen → Run). Es legt die Tabellen split_groups, split_group_members, split_expenses, split_expense_shares und split_settlements sowie Funktionen, Trigger und Row-Level-Security-Policies an. Alle Objekte haben das Präfix split_, die gemeinsam genutzte Tabelle users wird nur gelesen. Das Skript kann gefahrlos mehrfach ausgeführt werden.

Konfiguration

Lokal werden die Werte aus wwwroot/appsettings.Development.json gelesen:

{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "AnonKey": "your-anon-key"
  }
}

Für das Deployment auf GitHub Pages werden die Werte aus den Repository-Secrets SUPABASE_URL und SUPABASE_KEY beim Build in wwwroot/appsettings.json geschrieben (siehe .github/workflows/deploy.yml). Echte Schlüssel gehören nie in appsettings.json.

Damit Bestätigungs- und Passwort-Links funktionieren, die GitHub-Pages-URL in Supabase unter Authentication → URL Configuration als Site URL bzw. Redirect URL eintragen.

Anwendung starten

dotnet restore
dotnet run

Die Anwendung ist anschließend unter http://localhost:5046 verfügbar.

Tests ausführen

dotnet test tests/Split.Tests

Optionale Einrichtung

* Push-Benachrichtigungen: docs/push-notifications.md
* Veröffentlichung in App Stores: docs/app-stores.md

⸻

Architektur

Blazor WebAssembly
        │
        ▼
     Supabase
 ┌─────────────┐
 │ Auth        │
 │ PostgreSQL  │
 │ Realtime    │
 │ Storage     │
 └─────────────┘

Die Anwendung verfolgt einen clientzentrierten Ansatz, bei dem das Blazor-WebAssembly-Frontend direkt mit den von Supabase bereitgestellten Diensten kommuniziert.

⸻

Projektstruktur

* Core/ – Modelle (Datenbank-DTOs, Salden) und Interfaces
* Infrastructure/ – Supabase-Repositories (Datenbank, Storage, Push), Session-Persistenz und Offline-Speicher
* Services/ – Geschäftslogik: BalanceCalculator (Aufteilung, Salden, Ausgleich), Offline-Synchronisation, Realtime, Push
* Pages/ – Seiten (Dashboard, Gruppen, Gruppendetails, Einladung, Einstellungen, Anmeldung) und Dialoge
* Components/ – wiederverwendbare UI-Bausteine (Ladebildschirm, Statistiken, Betragsanzeige, Leerzustände)
* database/split_setup.sql – komplettes Datenbank-Setup (Tabellen, RPC-Funktionen, RLS, Storage, Realtime)
* supabase/functions/split-notify – Edge Function für Push-Benachrichtigungen
* tests/Split.Tests – Unit-Tests der Rechenlogik

⸻

Roadmap

* ✅ Gruppenverwaltung
* ✅ Mehrere Währungen (pro Gruppe und pro Ausgabe mit Umrechnung)
* ✅ Export von Abrechnungen (CSV)
* ✅ Wiederkehrende Ausgaben
* ✅ Push-Benachrichtigungen
* ✅ Offline-Synchronisation (neue Ausgaben und Zahlungen)
* ✅ Erweiterte Statistiken
* ✅ App-Store-Vorbereitung (Anleitung in docs/app-stores.md)
* Offline-Bearbeitung bestehender Einträge
* Maskierbare App-Icons und Store-Screenshots

⸻

Sicherheit

* Authentifizierung über Supabase Auth
* Sichere API-Kommunikation über HTTPS
* Datenzugriff über Row Level Security (RLS)
* Benutzerbezogene Zugriffskontrolle

⸻

Beitrag leisten

Beiträge, Verbesserungsvorschläge und Bug Reports sind willkommen.

1. Fork erstellen
2. Feature Branch anlegen
3. Änderungen committen
4. Pull Request erstellen

⸻

Lizenz

Dieses Projekt steht unter der MIT-Lizenz.

⸻

Lucas Nagelsmann

Entwickelt mit ❤️ für eine einfachere und transparentere Verwaltung gemeinsamer Ausgaben.