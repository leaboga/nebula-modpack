import os
import subprocess
import shutil

REPO_PATH = r"c:\Users\Leandro\source\repos\NebulaLauncher"
PROJECT_FILE = "KrakenLauncher.csproj"
VERSION = "3.0.7"
REMOTE_REPO = "leaboga/nebula-modpack"

def run_cmd(args):
    print(f"Executing: {' '.join(args)}")
    res = subprocess.run(args, cwd=REPO_PATH, capture_output=True, text=True)
    if res.returncode != 0:
        print(f"ERROR: {res.stderr}")
        return False
    print(res.stdout)
    return True

def publish_engine():
    # 1. Clean and Build
    print(f"--- Compilando Kraken Launcher v{VERSION} ---")
    if not run_cmd(["dotnet", "publish", PROJECT_FILE, "-c", "Release", "-r", "win-x64", "--self-contained", "true", "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true"]):
        return

    # 2. Path verification
    publish_dir = os.path.join(REPO_PATH, "bin", "Release", "net8.0-windows", "win-x64", "publish")
    original_exe = os.path.join(publish_dir, "KrakenLauncher.exe")
    if not os.path.exists(original_exe):
        original_exe = os.path.join(publish_dir, "Kraken.exe")

    target_exe = os.path.join(publish_dir, "KrakenLauncher.exe")

    if not os.path.exists(original_exe) and not os.path.exists(target_exe):
        print(f"ERROR: No se encontro el archivo compilado.")
        return

    # 3. Rename correctly for auto-updater
    if original_exe != target_exe and os.path.exists(original_exe):
        print(f"Renombrando {original_exe} -> {target_exe}")
        shutil.copy2(original_exe, target_exe)

    # 4. Push Code to Git
    print("--- Sincronizando codigo con GitHub ---")
    run_cmd(["git", "add", "."])
    run_cmd(["git", "commit", "-m", f"Release Engine v{VERSION} - Final Tabs and Theme Cleanup"])
    run_cmd(["git", "push", "origin", "main"])

    # 5. Create GitHub Release
    tag = f"v{VERSION}"
    print(f"--- Creando Release GitHub {tag} ---")
    subprocess.run(["git", "tag", "-d", tag], cwd=REPO_PATH)
    
    notes = f"UX EXACT REVISIONS (v{VERSION}).\n- Se elimino por completo la personalizacion de colores y presets del ConfigView, asumiendo la identidad visual Apple-like nativa.\n- Fix tecnico en ContentPresenter de Tabs Internas (HubView) para que los nombres 'Configs, Rendimiento, Comunidad, Mapa Abisal, etc.' se rendericen con texto y contraste visible.\n- Estandarizacion de headers internos en modulos con fondo oscuro estricto."
    
    # Use gh to create release and upload asset
    run_cmd(["gh", "release", "create", tag, target_exe, "--repo", REMOTE_REPO, "--title", f"KRAKEN Launcher v{VERSION}", "--notes", notes])

    print(f"\nPUBLICACION v{VERSION} COMPLETADA EXITOSAMENTE")

if __name__ == "__main__":
    publish_engine()
