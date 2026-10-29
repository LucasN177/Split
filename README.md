# Split

Split ist eine moderne Progressive Web App (PWA) zur einfachen Verwaltung gemeinsamer Ausgaben mit Freunden, Familie oder Gruppen. Die Anwendung hilft dabei, Kosten transparent zu erfassen, offene Beträge zu berechnen und Ausgleichszahlungen übersichtlich nachzuverfolgen.

Die Anwendung basiert auf Blazor WebAssembly als Frontend-Technologie und nutzt Supabase als Backend für Authentifizierung, Datenhaltung und Echtzeitfunktionen.

⸻

Features

* 💰 Gemeinsame Ausgaben erfassen
* 👥 Verwaltung von Gruppen und Teilnehmern
* 📊 Automatische Berechnung offener Salden
* 🔄 Nachverfolgung von Ausgleichszahlungen
* 📱 Progressive Web App (PWA) für mobile und Desktop-Nutzung
* 🔐 Sichere Authentifizierung über Supabase
* ☁️ Cloudbasierte Datenspeicherung
* ⚡ Moderne, performante Single-Page-Anwendung

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
* Infrastructure/ – Supabase-Repositories und Session-Persistenz (localStorage)
* Services/ – Geschäftslogik, u.a. BalanceCalculator für Aufteilung, Salden und minimale Ausgleichszahlungen
* Pages/ – Seiten (Dashboard, Gruppen, Gruppendetails, Einstellungen, Anmeldung) und Dialoge
* Components/ – wiederverwendbare UI-Bausteine (Ladebildschirm, Betragsanzeige, Leerzustände)
* database/split_setup.sql – komplettes Datenbank-Setup (Tabellen, Funktionen, RLS)

⸻

Roadmap

* ✅ Gruppenverwaltung
* ✅ Mehrere Währungen (pro Gruppe)
* ✅ Export von Abrechnungen (CSV)
* Wiederkehrende Ausgaben
* Push-Benachrichtigungen
* Offline-Synchronisation
* Erweiterte Statistiken
* Mobile App Stores (PWA Packaging)

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