# CLAUDE.md

Leitfaden für die Arbeit an diesem Repository. Ziel: sauberer, nachvollziehbarer Code nach gängigen C#- und Godot-Standards.

## Projekt

Gruppenarbeit (2 Personen) im Fach ADVAG: **3D k-d Tree mit interaktiver Visualisierung und Ray Casting**.

### Anforderungen (Angabe)

- **k-d-Tree-Aufbau**
  - Rekursiver Aufbau eines 3D k-d Trees aus der Szenengeometrie
  - Split am **Median** der Primitive des aktuellen Knotens
  - Split-Achse = **größte Ausdehnung** (längste Seite der Bounding Box des Knotens)
  - Dreiecke/Primitive werden **nur in Blattknoten** gespeichert
- **Interaktive 3D-Szene**
  - Szene mit mehreren Dreiecken/Meshes laden oder generieren
  - Freie Kamerabewegung (Rotation, Pan, Zoom)
  - Ein-/ausblendbares Wireframe-Overlay der k-d-Tree-Zellgrenzen
- **Ray Casting & Hit Detection**
  - Strahl in aktueller Blickrichtung durch den k-d Tree schießen
  - Nur relevante Zellen traversieren (leere/nicht geschnittene Knoten überspringen)
  - Erstes getroffenes Primitiv in eigener Farbe hervorheben

Jede Änderung soll sich auf eine dieser Anforderungen zurückführen lassen. Keine Features über die Angabe hinaus ohne Rückfrage.

## Tech-Stack

- **Godot 4.7** (Forward+, Jolt Physics, D3D12) mit **C# / .NET 8** (`Godot.NET.Sdk/4.7.2`)
- IDE: JetBrains Rider (`.idea/`)
- Hauptszene: `test_scene.tscn`, Einstieg: `Scripts/Scene/Main.cs`

### Build

```powershell
dotnet build KDTreeVis.sln
```

Nach jeder Code-Änderung muss der Build ohne Fehler und ohne neue Warnungen durchlaufen. Gestartet wird das Projekt über den Godot-Editor (oder `godot --path .`, falls Godot im PATH ist).

## Architektur

```
Scripts/
  KDTree/   Reine Datenstrukturen und Algorithmen (Triangle, Baum, Knoten, Ray, Traversierung)
  Scene/    Godot-Nodes: Szenenaufbau, Kamera, Mesh-Extraktion, Visualisierung, Input
```

- **Trennung von Algorithmus und Darstellung:** Code in `Scripts/KDTree/` erbt nicht von `Node`, greift nicht auf den Szenenbaum zu und erzeugt keine Meshes. Godot-Mathe-Typen (`Vector3`, `Aabb`) sind erlaubt. Visualisierung (Wireframes, Highlight) gehört nach `Scripts/Scene/`.
- **Datenfluss:** `MeshExtractor` → `Triangle[]` + Szenen-AABB (Weltkoordinaten) → k-d-Tree-Build → Visualisierung der Zellen / Ray-Traversierung → Highlight.
- Alle Geometrie im Baum liegt in **Weltkoordinaten**. Visualisierungs-Meshes, die Weltkoordinaten verwenden, setzen `TopLevel = true`.
- Neue Komponenten bekommen eine klare, einzelne Verantwortung (z. B. `KdTreeBuilder`, `KdTreeVisualizer`, `RayCaster`) statt alles in `Main.cs` zu sammeln. `Main` orchestriert nur.

## Algorithmische Vorgaben

### Aufbau

- Median über die **Schwerpunkte** (`Triangle.Centroid`) entlang der Split-Achse. Median per Quickselect (O(n)) oder Sortierung; die gewählte Variante im Code kurz begründen.
- Split-Achse über die längste Seite der **Knoten-Bounding-Box** (nicht der Gesamtszene).
- Dreiecke, die die Split-Ebene schneiden, werden in **beide** Kinder einsortiert (Referenzen/Indizes, keine Kopien der Geometrie). Damit ist die Traversierung korrekt.
- Abbruchkriterien explizit als benannte Konstanten bzw. `[Export]`-Parameter: maximale Tiefe, minimale Dreiecke pro Blatt, sowie Abbruch, wenn ein Split keinen Fortschritt bringt (ein Kind enthält alle Dreiecke).
- Innere Knoten speichern nur Achse, Split-Position, Bounds und Kinder; Blätter speichern Dreiecksindizes.
- Nach dem Build Statistiken loggen: Knotenanzahl, Blattanzahl, maximale Tiefe, durchschnittliche/maximale Dreiecke pro Blatt.

### Ray Casting

- **Eigene Implementierung verwenden** – kein `PhysicsRayQuery`, keine Kollisions-Shapes. Die Hit Detection muss über den k-d Tree laufen.
- Ray–AABB: Slab-Methode. Ray–Dreieck: Möller–Trumbore.
- Traversierung front-to-back: zuerst das nähere Kind, das fernere nur wenn nötig; Knoten ohne Schnitt oder ohne Dreiecke überspringen.
- Early Exit nur, wenn der gefundene Treffer **innerhalb des t-Intervalls der aktuellen Zelle** liegt (wegen Dreiecken in mehreren Zellen).
- Zum Testen eine Brute-Force-Referenz (alle Dreiecke prüfen) behalten und Ergebnisse vergleichen können.
- Floating-Point-Toleranzen als benannte Konstanten (`private const float ...Epsilon`), nie als Magic Numbers im Code.

## Coding Standards (C#)

### Formatierung & Benennung

- 4 Leerzeichen Einrückung, keine Tabs (`Triangle.cs` bei der nächsten Änderung angleichen), UTF-8.
- File-scoped Namespaces, Namespace entspricht dem Ordner: `KDTreeVis.Scene`, `KDTreeVis.KdTree` (bestehende Schreibweise beibehalten).
- Ein öffentlicher Typ pro Datei, Dateiname = Typname.
- `PascalCase` für Typen, Methoden, Properties, Konstanten; `_camelCase` für private Felder; `camelCase` für lokale Variablen und Parameter.
- Aussagekräftige Namen statt Abkürzungen (`splitAxis` statt `sa`). Etablierte Kürzel wie `t`, `v0`, `aabb` in der Geometrie sind ok.
- `var` nur, wenn der Typ aus der rechten Seite offensichtlich ist.
- Bestehende `using`-Reihenfolge: `System.*` zuerst, dann `Godot`, dann Projekt-Namespaces.

### Design

- Kleine Methoden mit einer Aufgabe; tiefe Verschachtelung durch Guard Clauses vermeiden.
- Unveränderliche Daten bevorzugen: `readonly struct` / `readonly record struct` für kleine Wertetypen (wie `Triangle`, `ExtractionResult`).
- Keine Magic Numbers: benannte Konstanten oder `[Export]`-Properties.
- Kein toter Code, keine auskommentierten Blöcke, keine ungenutzten `using`s.
- Keine neuen NuGet-Pakete oder Godot-Addons ohne Rückfrage.

### Kommentare & Doku

- Code, Bezeichner, Kommentare und Log-Ausgaben auf **Englisch**.
- Öffentliche Typen und Methoden bekommen `/// <summary>`-Doku.
- Kommentare erklären das **Warum** (z. B. Godot-Konventionen wie Winding Order, Bit-Kodierung von `Aabb.GetEndpoint`), nicht das Offensichtliche.

### Performance

- Baum einmalig beim Start (bzw. bei Szenenänderung) bauen, nie pro Frame.
- In `_Process` / `_PhysicsProcess` und in der Traversierung keine unnötigen Allokationen, kein LINQ, keine Rekursion mit Listen-Kopien – mit Index-Arrays arbeiten.
- Wireframe-Linien aller Knoten in **einem** `ArrayMesh` bündeln statt einer `MeshInstance3D` pro Knoten.

## Godot-Konventionen

- Node-Skripte sind `public partial class` und erben vom passenden Node-Typ.
- Konfigurierbare Werte als `[Export]`-Properties mit sinnvollen Defaults; Pflicht-Referenzen in `_Ready` prüfen und bei Fehlen `GD.PushError` mit Klassenname ausgeben.
- Input-Actions immer über die Input Map (`project.godot`) definieren und als `private static readonly StringName` referenzieren; keine hart codierten Keycodes. Neue Actions in `_Ready` auf Existenz prüfen (wie in `FlyCamera`).
- Discrete Aktionen (Toggles, Ray schießen) in `_UnhandledInput`, kontinuierliche Bewegung in `_Process`.
- `.tscn` und `project.godot` bevorzugt über den Editor ändern. Wenn sie per Hand geändert werden müssen: minimal und gezielt, bestehende UIDs nicht anfassen.
- `*.cs.uid`-Dateien gehören zum Skript und werden mit committet. `.godot/` wird nie committet.

## Tests & Verifikation

- Es gibt kein Test-Projekt. Korrektheit wird verifiziert durch:
  - erfolgreichen `dotnet build`,
  - Vergleich k-d-Tree-Treffer vs. Brute-Force-Treffer (gleiches Dreieck, gleiches `t`),
  - Invarianten-Checks im Debug (jedes Dreieck in mind. einem Blatt, Kind-Bounds liegen in Eltern-Bounds),
  - Sichtprüfung in der laufenden Szene.
- Wenn etwas nicht verifiziert werden konnte (z. B. Szene nicht gestartet), das explizit sagen.

## Git

- Nicht comitten
- Keine Änderungen an Dateien, die nichts mit der Aufgabe zu tun haben (insbesondere nicht an `.idea/`).
