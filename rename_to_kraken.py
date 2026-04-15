import os
import re

root_dir = "c:\\Users\\Leandro\\source\\repos\\NebulaLauncher"

replacements = {
    r"NebulaLauncher": "KrakenLauncher",
    r"Nebula Launcher": "KRAKEN Launcher",
    r"Nebula Hub": "KRAKEN Mod Hub",
    r"Nebula Mod Manager": "KRAKEN Mod Manager",
    r"Nebula Diagnostics": "KRAKEN Diagnostics",
    r"Nebula Screenshots": "KRAKEN Screenshots",
    r"Nebula Local Server": "KRAKEN Local Server",
    r"\[Nebula\]": "[KRAKEN]",
    r"diagnóstico Nebula": "diagnóstico KRAKEN",
    r"NebulaLoadingBar": "KrakenLoadingBar",
    r"nebula.ico": "kraken.ico",
    r"nebula_manifest.json": "kraken_manifest.json",
    r"nebula_pack.mrpack": "kraken_pack.mrpack",
}

def process_file(file_path):
    if file_path.endswith((".cs", ".xaml", ".csproj", ".md", ".json")):
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            new_content = content
            for old, new in replacements.items():
                new_content = re.sub(old, new, new_content, flags=re.IGNORECASE if " " in old else 0)
            
            if new_content != content:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Updated: {file_path}")
        except Exception as e:
            print(f"Error processing {file_path}: {e}")

for root, dirs, files in os.walk(root_dir):
    if ".git" in root or "bin" in root or "obj" in root:
        continue
    for file in files:
        process_file(os.path.join(root, file))
