#Split

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

Konfiguration

Erstelle die benötigten Konfigurationswerte für dein Supabase-Projekt:

{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "AnonKey": "your-anon-key"
  }
}

Anwendung starten

dotnet restore
dotnet run

Die Anwendung ist anschließend lokal verfügbar.

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

Roadmap

* Gruppenverwaltung
* Mehrere Währungen
* Wiederkehrende Ausgaben
* Push-Benachrichtigungen
* Offline-Synchronisation
* Export von Abrechnungen
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