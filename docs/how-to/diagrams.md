# Maintain the diagrams

Edit the Mermaid (`.mmd`) or PlantUML (`.puml`) sources under `docs/diagrams/`.
Regenerate the adjacent Scalable Vector Graphics (SVG) exports in the same change.
The documentation embeds those exports so readers can view the diagrams in both GitHub and the documentation site.
Keep the source links, captions, and [visual guide](../explanation/visual-guide.md) current.

## Prerequisites

The commands below use PowerShell from the repository root.
Install Node.js with npm, Python with pip, a Java runtime, and Google Chrome at the path shown below.
The browser runs headlessly to render Mermaid.
PlantUML uses its bundled Smetana layout engine, so a separate Graphviz installation is not required.

The renderer versions used for the committed exports are Mermaid CLI 11.12.0 and PlantUML 1.2025.2.
The documentation preview uses MkDocs Material 9.6.22.
Tool downloads go to a temporary folder and do not become application dependencies.

```powershell
$env:PUPPETEER_SKIP_DOWNLOAD = 'true'
$env:PUPPETEER_EXECUTABLE_PATH = 'C:/Program Files/Google/Chrome/Application/chrome.exe'
npm install --offline=false --prefix "$env:TEMP/RaidManager-diagram-tools" --cache "$env:TEMP/RaidManager-npm-cache" @mermaid-js/mermaid-cli@11.12.0 markdownlint-cli2@0.18.1
Invoke-WebRequest -Uri 'https://github.com/plantuml/plantuml/releases/download/v1.2025.2/plantuml-1.2025.2.jar' -OutFile "$env:TEMP/RaidManager-plantuml-1.2025.2.jar"
python -m pip install --isolated --index-url https://pypi.org/simple --target "$env:TEMP/RaidManager-docs-python" mkdocs-material==9.6.22
```

## Render source changes

Mermaid uses the shared font, palette, and spacing in
[mermaid-config.json](../diagrams/mermaid-config.json).
The following commands replace only generated SVG files beside their sources.

```powershell
$env:PUPPETEER_EXECUTABLE_PATH = 'C:/Program Files/Google/Chrome/Application/chrome.exe'
$renderer = Join-Path $env:TEMP 'RaidManager-diagram-tools/node_modules/.bin/mmdc.cmd'
foreach ($diagram in Get-ChildItem docs/diagrams -Filter *.mmd) {
    & $renderer -i $diagram.FullName -o ([System.IO.Path]::ChangeExtension($diagram.FullName, '.svg')) -c docs/diagrams/mermaid-config.json -b white
    if ($LASTEXITCODE -ne 0) {
        throw "Rendering failed: $($diagram.Name)"
    }
}
java '-Djava.awt.headless=true' -jar "$env:TEMP/RaidManager-plantuml-1.2025.2.jar" -tsvg -charset UTF-8 'docs/diagrams/*.puml'
if ($LASTEXITCODE -ne 0) {
    throw 'PlantUML rendering failed'
}
```

## Validate and preview

Check the rendered diagrams at full size for clipped text, crossed labels, and misleading arrow directions.
Read each conditional sequence from top to bottom, including failure branches.
Check multiplicities and ownership rules against the product scope and current source references.

```powershell
& "$env:TEMP/RaidManager-diagram-tools/node_modules/.bin/markdownlint-cli2.cmd" --no-globs 'docs/explanation/*.md' 'docs/how-to/*.md' 'docs/index.md' 'README.md'
$env:PYTHONPATH = "$env:TEMP/RaidManager-docs-python"
python -m mkdocs build --strict --site-dir "$env:TEMP/RaidManager-docs-site"
git diff --check
python -m mkdocs serve --dev-addr 127.0.0.1:8123
```

If port 8123 is already in use, choose a free local port.
Do not put production infrastructure in the current local development view.
Design diagrams must state their planned status until the implementation supports the illustrated behavior.

## Notation

Use [PlantUML use-case notation](https://plantuml.com/use-case-diagram) for UML actors and use cases.
Use the repository's approved Mermaid subset for domain, component, sequence, and state views.
Keep the full-size picture linked from its inline preview so large diagrams remain readable.
