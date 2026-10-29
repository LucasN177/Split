# Split in App Stores veröffentlichen

Split ist eine Progressive Web App. Für die Stores wird sie mit [PWABuilder](https://www.pwabuilder.com) in ein
natives Paket verpackt – der Code bleibt derselbe, Updates kommen weiterhin automatisch über GitHub Pages.

## Voraussetzungen

- App ist per HTTPS erreichbar (GitHub Pages: `https://<user>.github.io/Split/`).
- `manifest.webmanifest` und Service Worker sind vorhanden (bereits erledigt).
- **Empfohlen:** zusätzlich ein *maskierbares* Icon (512×512, Motiv innerhalb der inneren 80 %), in
  `manifest.webmanifest` mit `"purpose": "maskable"` eintragen. Android schneidet Icons sonst ggf. unschön zu.
- Screenshots (z.B. 1080×1920) für die Store-Einträge.

## Google Play (Android) – Trusted Web Activity

1. Auf pwabuilder.com die App-URL eingeben → *Package for stores* → **Android**.
2. Package-ID wählen (z.B. `de.innovapps.split`), Paket herunterladen.
3. Die mitgelieferte Datei `assetlinks.json` muss unter `https://<domain>/.well-known/assetlinks.json` erreichbar
   sein. **Wichtig:** Bei GitHub Pages unter `/Split/` geht das nicht – dafür ist eine eigene Domain
   (Custom Domain in den Pages-Einstellungen) oder ein `<user>.github.io`-Repository nötig.
4. Das `.aab` in der Google Play Console hochladen (einmalig 25 $ Entwicklergebühr).

## Microsoft Store (Windows)

1. pwabuilder.com → *Package for stores* → **Windows**.
2. Publisher-Daten aus dem Partner Center eintragen, Paket in Partner Center hochladen.

## Apple App Store (iOS)

1. pwabuilder.com → **iOS** erzeugt ein Xcode-Projekt (WKWebView-Hülle).
2. Mit Xcode auf einem Mac bauen und über App Store Connect einreichen (Apple Developer Program, 99 $/Jahr).
3. Hinweis: Apple lehnt reine Website-Hüllen teils ab (Guideline 4.2). Native Funktionen wie Push und
   Offline-Nutzung sollten im Review hervorgehoben werden.

## Ohne Store

Alle Plattformen können Split direkt aus dem Browser installieren („App installieren“ bzw. „Zum
Home-Bildschirm“). Push und Offline-Modus funktionieren auch so.
