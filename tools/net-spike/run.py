"""Modern-.NET feasibility spike driver. See README.md in this folder.

    python tools/net-spike/run.py <work-dir> [--tfm net10.0] [--smoke] [--coverage] [--only A,B]

Everything is generated under <work-dir> (keep it OUTSIDE the repository). No
repository source is modified: each legacy csproj is wrapped by an SDK-style
project that compiles the same source list by absolute path, and source files
can be replaced by patched copies (see PATCHES). Projects already converted to
SDK style are built in place, into the repository's git-ignored artifacts/
folder, and wrappers reference them. The coverage run takes its D4 libraries
from the runtime layout (artifacts/layout/debug/Libraries), which
tools/layout/Layout.csproj assembles.
"""
import argparse
import os
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True  # importing convert must not leave __pycache__ in the repository
from convert import compiled_files, is_converted

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))

# (repo-relative file, [(old, new), ...]) -- every patch here is a change the
# real port must make; docs/modernization-plan.md, Appendix A records them. Empty since
# build-server-project: the patches it carried either landed in the repository with the
# conversion of the project that compiles the file, or were dropped for the ticket that owns
# the change (K1 emitter: port-catalog-emitter-race; BEGIN IMMEDIATE: store-sqlite-concurrency).
PATCHES = []

LAYOUT = os.path.join(REPO, "tools", "layout", "Layout.csproj")

# D4 libraries the coverage run needs, copied with their assemblies from the runtime layout's Libraries folder
# to covlibs/<name> (the server finds libraries as immediate subfolders). A list rather than all of Libraries,
# so that the set of available libraries the coverage scripts see stays the same. Cov registers Connection and
# System.Internet after the scripts, to show that libraries with private package dependencies load from the layout.
COVERAGE_LIBS = ["Frontend", "SimpleDevice", "TestFramework.Coverage.Base", "SQLDevice", "MSSQLDevice", "Connection",
                 "TestFramework", "TestFramework.Coverage.Scripts", "System.Internet"]


# Project name -> absolute path of each listed project that is already SDK-style; filled by main().
CONVERTED = {}


def check_overrides():
    """A converted project compiles the repository's files, so a patch aimed at it would be ignored."""
    for name, proj in CONVERTED.items():
        files = compiled_files(proj)
        for rel, _ in PATCHES:
            if os.path.normcase(os.path.abspath(os.path.join(REPO, rel))) in files:
                sys.exit(f"PATCHES entry {rel} is compiled by {name}, which is SDK-style and builds in place "
                         "without it: land the change in the repository (or leave it to the ticket that owns it) "
                         "and remove the entry")


def apply_patches(out):
    for rel, pairs in PATCHES:
        src = os.path.join(REPO, rel)
        dst = os.path.join(out, "_overrides", rel)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        text = open(src, encoding="utf-8-sig", newline="").read()
        for old, new in pairs:
            if old not in text:
                sys.exit(f"patch no longer applies (already fixed in the repo?): {rel}: {old[:60]}")
            text = text.replace(old, new)
        open(dst, "w", encoding="utf-8", newline="").write(text)


def map_converted(template):
    r"""Point each ..\<Name>\<Name>.csproj reference at the real project file when <Name> is converted."""
    sep = chr(92)  # the templates use Windows separators
    for name, path in CONVERTED.items():
        template = template.replace(f"..{sep}{name}{sep}{name}.csproj", path)
    return template


def run(cmd, **kw):
    return subprocess.run(cmd, text=True, capture_output=True, **kw)


def build(out, name, proj=None):
    proj = proj or CONVERTED.get(name) or os.path.join(out, name, name + ".csproj")
    log = os.path.join(out, name + ".log")
    r = run(["dotnet", "build", proj, "-nologo", "-v", "q", "-clp:NoSummary"])
    open(log, "w", encoding="utf-8").write(r.stdout + r.stderr)
    s = run([sys.executable, os.path.join(HERE, "summ.py"), log, "8"])
    print(s.stdout.replace(out + os.sep, "").rstrip())
    return r.returncode == 0


def find_dll(out, project, dll):
    for root, _, files in os.walk(os.path.join(out, project, "bin")):
        if dll in files:
            return os.path.join(root, dll)
    sys.exit(f"{dll} not built")


def layout_libraries():
    # Only MSBuild knows where the layout is (DataphorLayoutDir, Directory.Build.targets).
    r = run(["dotnet", "msbuild", LAYOUT, "-nologo", "-getProperty:DataphorLayoutDir", "-p:Configuration=Debug"])
    path = os.path.join(r.stdout.strip(), "Libraries")
    if r.returncode != 0 or not os.path.isdir(path):
        sys.exit(f"runtime layout not built (MSBuild reports DataphorLayoutDir {r.stdout.strip()!r})")
    return path


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("work")
    ap.add_argument("--tfm", default="net10.0")
    ap.add_argument("--smoke", action="store_true", help="boot the engine in-process and run D4, incl. concurrent DDL")
    ap.add_argument("--coverage", action="store_true", help="run the D4 coverage scripts listed in tests/DAE.Server.Tests/CoverageTest.cs")
    ap.add_argument("--only", help="comma list of projects to build (default: all in projects.txt)")
    a = ap.parse_args()
    work = os.path.abspath(a.work)
    if os.path.commonpath([work, REPO]) == REPO:
        sys.exit("work-dir must be outside the repository")
    out = os.path.join(work, "out")
    os.makedirs(out, exist_ok=True)

    projects = [l.strip() for l in open(os.path.join(HERE, "projects.txt")) if l.strip()]
    for p in projects:
        path = os.path.join(REPO, *p.split("/"))
        if is_converted(path):
            CONVERTED[os.path.splitext(os.path.basename(p))[0]] = path
    check_overrides()
    apply_patches(out)
    r = run([sys.executable, os.path.join(HERE, "convert.py"), REPO, out, a.tfm] + projects)
    print(r.stdout + r.stderr)
    for tool in ("Smoke", "Cov"):
        shutil.copytree(os.path.join(HERE, tool), os.path.join(out, tool), dirs_exist_ok=True)
        # Stored as .csproj.txt so IDEs and `dotnet build` at the repo root do not pick them up.
        os.remove(os.path.join(out, tool, tool + ".csproj.txt"))
        template = open(os.path.join(HERE, tool, tool + ".csproj.txt"), encoding="utf-8").read()
        # Templates name projects as ..\<Name>\<Name>.csproj, the wrappers' layout; converted ones build in place.
        template = map_converted(template).replace("net8.0", a.tfm)
        open(os.path.join(out, tool, tool + ".csproj"), "w", encoding="utf-8").write(template)

    names = [os.path.splitext(os.path.basename(p))[0] for p in projects]
    if a.only:
        names = [n for n in names if n in a.only.split(",")]
    for n in names:
        build(out, n)

    rundir = os.path.join(work, "run")
    os.makedirs(rundir, exist_ok=True)
    if a.smoke:
        if build(out, "Smoke"):
            env = dict(os.environ, SMOKE_CS=";Journal Mode=WAL;BusyTimeout=30000")
            subprocess.run(["dotnet", find_dll(out, "Smoke", "Smoke.dll"), rundir, "sqlite", "8", "15"], env=env)
    if a.coverage:
        libs = os.path.join(rundir, "covlibs")
        shutil.rmtree(libs, ignore_errors=True)
        if not build(out, "Layout", LAYOUT):
            sys.exit("runtime layout build failed; see Layout.log in the work dir")
        layout = layout_libraries()
        for name in COVERAGE_LIBS:
            shutil.copytree(os.path.join(layout, name), os.path.join(libs, name))
        if build(out, "Cov"):
            subprocess.run(["dotnet", find_dll(out, "Cov", "Cov.dll"), rundir,
                            os.path.join(REPO, "tests", "DAE.Server.Tests", "CoverageTest.cs"), "base"])


if __name__ == "__main__":
    main()
