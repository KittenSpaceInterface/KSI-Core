#!/usr/bin/env bash
set -euo pipefail

repository_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
cd "$repository_dir"

if ! command -v cygpath >/dev/null 2>&1; then
    echo 'Git Bash with cygpath is required on Windows.' >&2
    exit 1
fi

ksa_reference_path="${KSA_REFERENCE_PATH:-C:/Jogos/Kitten Space Agency}"
ksa_reference_posix="$(cygpath -u "$ksa_reference_path")"
if [[ ! -f "$ksa_reference_posix/KSA.dll" ]]; then
    echo "KSA reference assemblies not found in: $ksa_reference_path" >&2
    exit 1
fi
ksa_reference_native="$(cygpath -w "$ksa_reference_posix")"

if [[ -n "${KSI_CORE_MOD_DIR:-}" ]]; then
    install_dir="$(cygpath -u "$KSI_CORE_MOD_DIR")"
else
    install_dir="$(cygpath -u "$USERPROFILE")/Documents/My Games/Kitten Space Agency/mods/KSI-Core"
fi

dotnet restore KSI.Core.slnx "-p:KsaReferencePath=$ksa_reference_native"
dotnet build KSI.Core.slnx --configuration Release --no-restore "-p:KsaReferencePath=$ksa_reference_native"
dotnet run --project tests/KSI.Core.Tests --configuration Release --no-build "-p:KsaReferencePath=$ksa_reference_native"
dotnet pack src/KSI.Core.Api/KSI.Core.Api.csproj --configuration Release --no-build \
    --output artifacts/packages "-p:KsaReferencePath=$ksa_reference_native"

build_dir="$repository_dir/src/KSI.Core.Mod/bin/Release/net10.0"
for name in KSI.Core.Mod.dll KSI.Core.Mod.deps.json KSI.Core.dll KSI.Core.Api.dll mod.toml; do
    [[ -f "$build_dir/$name" ]] || { echo "Missing build output: $name" >&2; exit 1; }
done

mkdir -p "$install_dir"
for name in KSI.Core.Mod.dll KSI.Core.Mod.deps.json KSI.Core.dll KSI.Core.Api.dll mod.toml; do
    cp -f -- "$build_dir/$name" "$install_dir/$name"
done
cmp -s -- "$build_dir/KSI.Core.Mod.dll" "$install_dir/KSI.Core.Mod.dll"
echo "Installed KSI-Core in: $install_dir"
echo 'Existing provider and host settings were left in place.'
