import argparse
import re
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BASELINE = "d934eb4d433b1e8fd9d943783d6b0b5489c76b7e"
HUD_FIELDS = (
    "stageLabelText", "shotsText", "destructionText", "destructionFill",
    "endPanel", "resultTitleText", "resultDetailsText", "nextButton",
)
FEEDBACK_FIELDS = (
    "cameraShake", "muzzleFlash", "muzzleSmoke", "impactDust", "impactSparks",
    "impactDebris", "structuralImpactDust", "collapseDust", "audioHooks",
)


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def baseline_file(revision, name):
    return subprocess.check_output(["git", "show", f"{revision}:{name}"], cwd=ROOT)


def scene_blocks(content):
    blocks = {}
    for section in re.split(r"(?m)(?=^--- !u!)", content)[1:]:
        identifier = re.match(r"--- !u!\d+ &(-?\d+)", section).group(1)
        require(-(2 ** 63) <= int(identifier) < 2 ** 63, f"Scene fileID outside signed 64-bit range: {identifier}")
        require(identifier not in blocks, f"Duplicate scene fileID: {identifier}")
        blocks[identifier] = section
    for reference in re.findall(r"fileID: (-?\d+)", content):
        require(-(2 ** 63) <= int(reference) < 2 ** 63, f"Reference outside signed 64-bit range: {reference}")
    return blocks


def script_guid(name):
    return re.search(r"^guid: (\w+)$", name.read_text(), re.M).group(1)


def script_block(blocks, guid):
    matches = [block for block in blocks.values() if f"guid: {guid}," in block]
    require(len(matches) == 1, f"Expected exactly one component for {guid}")
    return matches[0]


def field_line(block, name):
    match = re.search(r"^  " + name + r":[^\n]*\n", block, re.M)
    require(match is not None, f"Missing serialized field {name}")
    return match.group()


def verify_scenes(revision):
    stage_guid = script_guid(ROOT / "Assets/Scripts/Gameplay/Stage/VerticalSliceStageController.cs.meta")
    hud_guid = script_guid(ROOT / "Assets/Scripts/Presentation/UI/StageHudView.cs.meta")
    feedback_guid = script_guid(ROOT / "Assets/Scripts/Presentation/Feedback/StageFeedbackController.cs.meta")
    checked_blocks = 0
    for stage in range(1, 6):
        name = f"Assets/Scenes/VerticalSlice_Stage{stage:02}.unity"
        before = scene_blocks(baseline_file(revision, name).decode())
        after = scene_blocks((ROOT / name).read_text())
        previous = script_block(before, stage_guid)
        current = script_block(after, stage_guid)
        hud = script_block(after, hud_guid)
        feedback = script_block(after, feedback_guid)
        stage_id = re.match(r"--- !u!114 &(\d+)", current).group(1)
        owner = re.search(r"  m_GameObject: \{fileID: (\d+)\}", current).group(1)
        added = set(after) - set(before)
        require(len(added) == 2 and len(after) == len(before) + 2, f"Unexpected scene additions: {name}")
        for component, fields, reference_name in (
            (hud, HUD_FIELDS, "hud"), (feedback, FEEDBACK_FIELDS, "feedback"),
        ):
            component_id = re.match(r"--- !u!114 &(\d+)", component).group(1)
            require(component_id in added, f"Presentation component did not receive a new ID: {name}")
            require(field_line(component, "m_GameObject") == field_line(current, "m_GameObject"), f"Different owner: {name}")
            require(field_line(current, reference_name) == f"  {reference_name}: {{fileID: {component_id}}}\n", f"Missing component reference: {name}")
            require(f"  - component: {{fileID: {component_id}}}\n" in after[owner], f"Missing owner attachment: {name}")
            for field in fields:
                require(field_line(component, field) == field_line(previous, field), f"Changed reference: {name} {field}")
                target = re.search(r"fileID: (\d+)", field_line(component, field)).group(1)
                require(target != "0" and target in after, f"Unresolved reference: {name} {field}")
        stripped_previous = previous
        for field in HUD_FIELDS + FEEDBACK_FIELDS:
            stripped_previous = stripped_previous.replace(field_line(previous, field), "")
        stripped_current = current.replace(field_line(current, "hud"), "").replace(field_line(current, "feedback"), "")
        require(stripped_previous == stripped_current, f"Stage configuration changed: {name}")
        stripped_owner = after[owner]
        for identifier in added:
            stripped_owner = stripped_owner.replace(f"  - component: {{fileID: {identifier}}}\n", "")
        require(before[owner] == stripped_owner, f"Stage owner changed: {name}")
        for identifier, block in before.items():
            if identifier in (stage_id, owner):
                continue
            require(after.get(identifier) == block, f"Physics/presentation object changed: {name} {identifier}")
            checked_blocks += 1
    print(f"PASS: Stage01–05; {checked_blocks} original object blocks unchanged; 85 presentation references preserved.")


def verify_assets(revision):
    tracked = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", revision], cwd=ROOT).decode().splitlines()
    meta_files = list((ROOT / "Assets/Scripts").rglob("*.meta")) + list((ROOT / "Assets/Editor").rglob("*.meta"))
    guids = {}
    for meta in meta_files:
        guid = script_guid(meta)
        require(guid not in guids, f"Duplicate GUID: {meta}")
        guids[guid] = meta
    original_scripts = 0
    protected_files = 0
    for name in tracked:
        if name.startswith(("Assets/Scripts/", "Assets/Editor/")) and name.endswith(".cs.meta"):
            expected = baseline_file(revision, name)
            guid = re.search(rb"guid: (\w+)", expected).group(1).decode()
            require(guid in guids and guids[guid].read_bytes() == expected, f"Original script metadata changed: {name}")
            original_scripts += 1
        if name.startswith(("ProjectSettings/", "Packages/", "Assets/Prefabs/")) or (
            name.startswith("Assets/Scenes/") and not re.fullmatch(r"Assets/Scenes/VerticalSlice_Stage0[1-5]\.unity", name)
        ):
            require((ROOT / name).read_bytes() == baseline_file(revision, name), f"Protected asset changed: {name}")
            protected_files += 1
    print(f"PASS: {original_scripts} original script metas; {protected_files} protected files; unique script/folder GUIDs.")


def verify_core_source(revision):
    for filename in ("LauncherController", "Projectile", "StageFlowStateMachine", "StageTargetPiece", "CriticalSupportJoint"):
        matches = list((ROOT / "Assets/Scripts").rglob(filename + ".cs"))
        require(len(matches) == 1, f"Duplicate core source: {filename}")
        require(matches[0].read_bytes() == baseline_file(revision, f"Assets/Scripts/{filename}.cs"), f"Core behavior source changed: {filename}")
    for document in (ROOT / "README.md", ROOT / "docs/interview-defense.md"):
        for link in re.findall(r"\]\(([^)]+\.cs)\)", document.read_text()):
            require((document.parent / link).is_file(), f"Broken source link: {link}")
    print("PASS: launcher, projectile, state machine, target rule, joint callback source unchanged; source links resolve.")


def method_source(content, name):
    match = re.search(
        r"^        (?:private|public|internal) (?:static )?[^\n=;]+?\b"
        + name + r"\([\s\S]*?^        \}\n", content, re.M,
    )
    require(match is not None, f"Missing method: {name}")
    return re.sub(r"\s+", "", match.group())


def verify_preserved_methods(revision):
    stage_before = baseline_file(revision, "Assets/Scripts/VerticalSliceStageController.cs").decode()
    stage_after = (ROOT / "Assets/Scripts/Gameplay/Stage/VerticalSliceStageController.cs").read_text()
    stage_methods = (
        "Start", "OnEnable", "OnDisable", "InitializeTargets", "ActivateGameplayNextFrame",
        "BeginSettling", "RetryStage", "LoadNextStage", "CountDestroyedTargets",
        "CalculateDestructionPercentage", "LogInitialTargetState", "LockInClear",
    )
    for name in stage_methods:
        require(method_source(stage_before, name) == method_source(stage_after, name), f"Stage lifecycle/rule changed: {name}")
    research_before = baseline_file(revision, "Assets/Scripts/StructureManager.cs").decode()
    research_after = (ROOT / "Assets/Scripts/Research/StructuralSimulation/StructureManager.cs").read_text()
    research_methods = (
        "DestroyBlock", "InitializeGraph", "HandlePhysicalConnectionBreak",
        "RebuildActiveAdjacency", "EvaluateConnections", "DeactivateConnectionsFor",
    )
    for name in research_methods:
        require(method_source(research_before, name) == method_source(research_after, name), f"Research lifecycle/rule changed: {name}")
    print(f"PASS: {len(stage_methods) + len(research_methods)} lifecycle/rule methods preserved (ignoring whitespace).")


def main():
    parser = argparse.ArgumentParser(description="Static refactoring regression checks; does not run Unity.")
    parser.add_argument("--baseline", default=BASELINE)
    arguments = parser.parse_args()
    verify_scenes(arguments.baseline)
    verify_assets(arguments.baseline)
    verify_core_source(arguments.baseline)
    verify_preserved_methods(arguments.baseline)


if __name__ == "__main__":
    main()
