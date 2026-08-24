# Task 4 — Stage Session, Boss Mechanic, And Three-Star Vertical-Slice Rules

## Delivered

- Added Unity-independent stage catalog/session runtime with catalog boundary validation, deep authored-data snapshots, lazy ordered wave instantiation, safe current-wave/enemy access, permanent-story `ConsumesStamina == false`, and completion locking.
- Added immutable `StageResult`, detached/order-preserving `StarResult`, and independent inclusive clear/HP/resolution star evaluation.
- Added threshold enrage evaluation from generic `EnemyEffectData`. The explicit `EnemyThresholdTriggerData.HpThresholdPercent` schema owns threshold metadata; each trigger applies once per enemy runtime while stronger existing multiplier and longer duration are preserved.
- Exercised existing generic `ComboShield` enemy runtime/effect behavior through an authored action; no boss subclass or special-case shield type was added.

## Test-first record

### Initial RED

Command:

```powershell
$unityMono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$nunitPath = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$coreFiles = Get-ChildItem 'Assets\PuzzleGame\Core\Contracts','Assets\PuzzleGame\Core\Board','Assets\PuzzleGame\Core\Battle' -Filter '*.cs' | ForEach-Object { $_.FullName }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Core.stage-red.dll' $coreFiles
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Tests.EditMode.Stage.red.dll' -r:'Temp\PuzzleGame.Core.stage-red.dll' -r:$nunitPath 'Assets\PuzzleGame\Tests\EditMode\Stages\StageSessionTests.cs'
exit $LASTEXITCODE
```

Output (exit 1):

```text
StageSessionTests.cs(8,23): error CS0234: The type or namespace name `Stages' does not exist in the namespace `PuzzleGame.Core'.
Compilation failed: 1 error(s), 0 warnings
```

### Focused behavioral RED

The duplicate-report regression was added before its production guard.

Command:

```powershell
$unityMono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$nunitPath = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$coreFiles = Get-ChildItem 'Assets\PuzzleGame\Core\Contracts','Assets\PuzzleGame\Core\Board','Assets\PuzzleGame\Core\Battle','Assets\PuzzleGame\Core\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Core.stage-dedup-red.dll' $coreFiles
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Tests.EditMode.Stage.dedup-red.dll' -r:'Temp\PuzzleGame.Core.stage-dedup-red.dll' -r:$nunitPath 'Assets\PuzzleGame\Tests\EditMode\Stages\StageSessionTests.cs'
[void][Reflection.Assembly]::LoadFrom($nunitPath)
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path (Get-Location) 'Temp\PuzzleGame.Tests.EditMode.Stage.dedup-red.dll'))
$type = $assembly.GetType('PuzzleGame.Tests.EditMode.Stages.StageSessionTests', $true)
$method = $type.GetMethod('Session_counts_only_a_completed_non_defeated_battle_resolution')
try { $method.Invoke([Activator]::CreateInstance($type), @()); Write-Output 'UNEXPECTED PASS'; exit 1 }
catch { Write-Output "EXPECTED RED: $($_.Exception.InnerException.Message)" }
```

Output (exit 0 because the expected failure was caught):

```text
EXPECTED RED:   Expected: 1
  But was:  2
```

The negative threshold-metadata boundary was also added before its validator branch.

Command:

```powershell
$unityMono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$nunitPath = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$coreFiles = Get-ChildItem 'Assets\PuzzleGame\Core\Contracts','Assets\PuzzleGame\Core\Board','Assets\PuzzleGame\Core\Battle','Assets\PuzzleGame\Core\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Core.stage-threshold-red.dll' $coreFiles
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Tests.EditMode.Stage.threshold-red.dll' -r:'Temp\PuzzleGame.Core.stage-threshold-red.dll' -r:$nunitPath 'Assets\PuzzleGame\Tests\EditMode\Stages\StageSessionTests.cs'
[void][Reflection.Assembly]::LoadFrom($nunitPath)
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path (Get-Location) 'Temp\PuzzleGame.Tests.EditMode.Stage.threshold-red.dll'))
$type = $assembly.GetType('PuzzleGame.Tests.EditMode.Stages.StageSessionTests', $true)
$method = $type.GetMethod('Catalog_rejects_duplicate_wave_ids_and_invalid_star_boundaries')
try { $method.Invoke([Activator]::CreateInstance($type), @()); Write-Output 'UNEXPECTED PASS'; exit 1 }
catch { Write-Output "EXPECTED RED: $($_.Exception.InnerException.Message)" }
```

Output:

```text
EXPECTED RED:   Expected: <System.ArgumentException>
  But was:  no exception thrown
```

### Mutation check

The duplicate-wave predicate was temporarily changed from `if (!waveIds.Add(wave.Id)) throw` to `waveIds.Add(wave.Id);`, then restored.

Output:

```text
MUTANT KILLED:   Expected: <System.ArgumentException>
  But was:  no exception thrown
```

## Focused GREEN

Command:

```powershell
$unityMono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$nunitPath = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$coreFiles = Get-ChildItem 'Assets\PuzzleGame\Core\Contracts','Assets\PuzzleGame\Core\Board','Assets\PuzzleGame\Core\Battle','Assets\PuzzleGame\Core\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Core.stage-final.dll' $coreFiles
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Tests.EditMode.Stage.final.dll' -r:'Temp\PuzzleGame.Core.stage-final.dll' -r:$nunitPath 'Assets\PuzzleGame\Tests\EditMode\Stages\StageSessionTests.cs'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
[void][Reflection.Assembly]::LoadFrom($nunitPath)
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path (Get-Location) 'Temp\PuzzleGame.Tests.EditMode.Stage.final.dll'))
$type = $assembly.GetType('PuzzleGame.Tests.EditMode.Stages.StageSessionTests', $true)
$failed=0; $total=0
foreach($method in $type.GetMethods() | Where-Object { $_.GetCustomAttributes($true) | Where-Object { $_.GetType().FullName -eq 'NUnit.Framework.TestAttribute' -or $_.GetType().FullName -eq 'NUnit.Framework.TestCaseAttribute' } }) {
  $cases = $method.GetCustomAttributes($true) | Where-Object { $_.GetType().FullName -eq 'NUnit.Framework.TestCaseAttribute' }
  if ($cases.Count -eq 0) { $cases = @($null) }
  foreach($case in $cases) { $total++; try { $arguments = if ($null -eq $case) { @() } else { @($case.Arguments) }; $method.Invoke([Activator]::CreateInstance($type), $arguments) } catch { $failed++; Write-Output "FAIL $($method.Name): $($_.Exception.InnerException.Message)" } }
}
Write-Output "SUMMARY total=$total passed=$($total-$failed) failed=$failed"
if($failed -gt 0){exit 1}
```

Output (exit 0, no compiler warnings/errors):

```text
SUMMARY total=19 passed=19 failed=0
```

## Full direct real-NUnit GREEN

Command:

```powershell
$unityMono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$nunitPath = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$coreFiles = Get-ChildItem 'Assets\PuzzleGame\Core\Contracts','Assets\PuzzleGame\Core\Board','Assets\PuzzleGame\Core\Battle','Assets\PuzzleGame\Core\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
$testFiles = Get-ChildItem 'Assets\PuzzleGame\Tests\EditMode\Contracts','Assets\PuzzleGame\Tests\EditMode\Board','Assets\PuzzleGame\Tests\EditMode\Battle','Assets\PuzzleGame\Tests\EditMode\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Core.full-task4.dll' $coreFiles
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Tests.EditMode.full-task4.dll' -r:'Temp\PuzzleGame.Core.full-task4.dll' -r:$nunitPath $testFiles
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
[void][Reflection.Assembly]::LoadFrom($nunitPath)
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path (Get-Location) 'Temp\PuzzleGame.Tests.EditMode.full-task4.dll'))
$failed = 0; $total = 0
$typeNames = @('PuzzleGame.Tests.EditMode.Contracts.ContractValidationTests','PuzzleGame.Tests.EditMode.Board.BoardTests','PuzzleGame.Tests.EditMode.Board.BoardResolverTests','PuzzleGame.Tests.EditMode.Battle.CombatTests','PuzzleGame.Tests.EditMode.Battle.EnemyAndSkillTests','PuzzleGame.Tests.EditMode.Stages.StageSessionTests')
foreach ($typeName in $typeNames) {
  $type = $assembly.GetType($typeName, $true); $typeTotal = 0; $typeFailed = 0
  foreach ($method in $type.GetMethods() | Where-Object { $_.GetCustomAttributes($true) | Where-Object { $_.GetType().FullName -eq 'NUnit.Framework.TestAttribute' -or $_.GetType().FullName -eq 'NUnit.Framework.TestCaseAttribute' } }) {
    $cases = $method.GetCustomAttributes($true) | Where-Object { $_.GetType().FullName -eq 'NUnit.Framework.TestCaseAttribute' }
    if ($cases.Count -eq 0) { $cases = @($null) }
    foreach ($case in $cases) { $total++; $typeTotal++; try { $arguments = if ($null -eq $case) { @() } else { @($case.Arguments) }; $method.Invoke([Activator]::CreateInstance($type), $arguments) } catch { $failed++; $typeFailed++; Write-Output "FAIL $($type.Name).$($method.Name): $($_.Exception.InnerException.Message)" } }
  }
  Write-Output "$($type.Name): passed=$($typeTotal-$typeFailed) failed=$typeFailed"
}
Write-Output "SUMMARY total=$total passed=$($total-$failed) failed=$failed"
if ($failed -gt 0) { exit 1 }
```

Output (exit 0, no compiler warnings/errors):

```text
ContractValidationTests: passed=25 failed=0
BoardTests: passed=14 failed=0
BoardResolverTests: passed=7 failed=0
CombatTests: passed=15 failed=0
EnemyAndSkillTests: passed=29 failed=0
StageSessionTests: passed=19 failed=0
SUMMARY total=109 passed=109 failed=0
```

## Files

- `Assets/PuzzleGame/Core/Contracts/ContentContracts.cs`
- `Assets/PuzzleGame/Core/Battle/EnemyDataValidation.cs`
- `Assets/PuzzleGame/Core/Battle/EnemyRuntime.cs`
- `Assets/PuzzleGame/Core/Battle/EnemyActionEngine.cs`
- `Assets/PuzzleGame/Core/Battle/CharacterRuntime.cs`
- `Assets/PuzzleGame/Core/Stages/StageCatalog.cs`
- `Assets/PuzzleGame/Core/Stages/StageSession.cs`
- `Assets/PuzzleGame/Core/Stages/StageObjectiveEvaluator.cs`
- `Assets/PuzzleGame/Core/Stages/BossMechanicEngine.cs`
- `Assets/PuzzleGame/Tests/EditMode/Battle/EnemyAndSkillTests.cs`
- `Assets/PuzzleGame/Tests/EditMode/Stages/StageSessionTests.cs`

## Self-review

- Catalog rejects blank/duplicate stage, wave, and enemy IDs; rejects unknown wave enemy references; validates objective bounds and runtime-safe enemy graphs before accepting authoring data.
- Catalog/session/public result boundaries deep-copy authored DTOs. Session materializes only the current wave, in order; its public enemy collection cannot change membership, and runtime data snapshots remain detached.
- Completion blocks later advance, board-resolution recording, and engine action. The result-instance guard prevents the same completed battle outcome being counted twice; counters saturate at `Int32.MaxValue`.
- Objective evaluation preserves authored order, copies objectives per result, validates `StageResult` ratios/counts, and uses inclusive HP/count limits.
- Threshold behavior is generic `EnemyEffectData` and tracks every authored threshold effect separately per runtime. No existing battle contracts or battle behavior were changed.

## Concerns

- Content that previously used a nonzero enrage `Amount` as threshold metadata must migrate to explicit `ThresholdTriggers`; nonzero Amount on an ordinary action remains ordinary action data.

---

## Fix round 1 — Explicit threshold schema and centralized enemy validation

### Delivered

- Added serializable `EnemyThresholdTriggerData` and `EnemyData.ThresholdTriggers` with an empty safe default. Thresholds are no longer encoded in `EnemyEffectData.Payload.Amount`; countdown actions remain action-only.
- `BossMechanicEngine` now reads only explicit threshold triggers. Trigger consumption is owned by `EnemyRuntime`, so a fresh mechanic engine cannot reapply a consumed trigger. Trigger data is included in every authored enemy snapshot.
- Added a single `EnemyDataValidation` authority used by catalog/session boundaries, `EnemyRuntime` construction, `EnemyActionEngine`, and threshold triggers. It validates action/effect graphs and all effect-family semantics before gameplay.
- Updated the two prior battle tests whose old expectation conflicted with the new required constructor-time validation boundary.

### RED

Command:

```powershell
$unityMono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$nunitPath = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$coreFiles = Get-ChildItem 'Assets\PuzzleGame\Core\Contracts','Assets\PuzzleGame\Core\Board','Assets\PuzzleGame\Core\Battle','Assets\PuzzleGame\Core\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Core.task4-fix-red.dll' $coreFiles
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Tests.EditMode.Stage.task4-fix-red.dll' -r:'Temp\PuzzleGame.Core.task4-fix-red.dll' -r:$nunitPath 'Assets\PuzzleGame\Tests\EditMode\Stages\StageSessionTests.cs'
exit $LASTEXITCODE
```

Output (exit 1):

```text
StageSessionTests.cs(381,25): error CS0246: The type or namespace name `EnemyThresholdTriggerData' could not be found.
Compilation failed: 1 error(s), 0 warnings
```

### Focused GREEN

Command:

```powershell
$unityMono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$nunitPath = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$coreFiles = Get-ChildItem 'Assets\PuzzleGame\Core\Contracts','Assets\PuzzleGame\Core\Board','Assets\PuzzleGame\Core\Battle','Assets\PuzzleGame\Core\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Core.task4-fix.dll' $coreFiles
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Tests.EditMode.Stage.task4-fix.dll' -r:'Temp\PuzzleGame.Core.task4-fix.dll' -r:$nunitPath 'Assets\PuzzleGame\Tests\EditMode\Stages\StageSessionTests.cs'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
[void][Reflection.Assembly]::LoadFrom($nunitPath)
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path (Get-Location) 'Temp\PuzzleGame.Tests.EditMode.Stage.task4-fix.dll'))
$type = $assembly.GetType('PuzzleGame.Tests.EditMode.Stages.StageSessionTests', $true)
$failed=0;$total=0
foreach($method in $type.GetMethods() | Where-Object { $_.GetCustomAttributes($true) | Where-Object { $_.GetType().FullName -eq 'NUnit.Framework.TestAttribute' -or $_.GetType().FullName -eq 'NUnit.Framework.TestCaseAttribute' } }) {
  $cases=$method.GetCustomAttributes($true) | Where-Object { $_.GetType().FullName -eq 'NUnit.Framework.TestCaseAttribute' }; if($cases.Count -eq 0){$cases=@($null)}
  foreach($case in $cases){$total++;try{$args=if($null -eq $case){@()}else{@($case.Arguments)};$method.Invoke([Activator]::CreateInstance($type),$args)}catch{$failed++;Write-Output "FAIL $($method.Name): $($_.Exception.InnerException.Message)"}}
}
Write-Output "SUMMARY total=$total passed=$($total-$failed) failed=$failed"
if($failed -gt 0){exit 1}
```

Output (exit 0, no compiler warnings/errors):

```text
SUMMARY total=25 passed=25 failed=0
```

### Full direct real-NUnit GREEN

Command:

```powershell
$unityMono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$nunitPath = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$coreFiles = Get-ChildItem 'Assets\PuzzleGame\Core\Contracts','Assets\PuzzleGame\Core\Board','Assets\PuzzleGame\Core\Battle','Assets\PuzzleGame\Core\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
$testFiles = Get-ChildItem 'Assets\PuzzleGame\Tests\EditMode\Contracts','Assets\PuzzleGame\Tests\EditMode\Board','Assets\PuzzleGame\Tests\EditMode\Battle','Assets\PuzzleGame\Tests\EditMode\Stages' -Filter '*.cs' | ForEach-Object { $_.FullName }
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Core.full-task4-fix.dll' $coreFiles
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $unityMono -warn:4 -target:library -out:'Temp\PuzzleGame.Tests.EditMode.full-task4-fix.dll' -r:'Temp\PuzzleGame.Core.full-task4-fix.dll' -r:$nunitPath $testFiles
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
[void][Reflection.Assembly]::LoadFrom($nunitPath)
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path (Get-Location) 'Temp\PuzzleGame.Tests.EditMode.full-task4-fix.dll'))
$failed=0;$total=0
$typeNames=@('PuzzleGame.Tests.EditMode.Contracts.ContractValidationTests','PuzzleGame.Tests.EditMode.Board.BoardTests','PuzzleGame.Tests.EditMode.Board.BoardResolverTests','PuzzleGame.Tests.EditMode.Battle.CombatTests','PuzzleGame.Tests.EditMode.Battle.EnemyAndSkillTests','PuzzleGame.Tests.EditMode.Stages.StageSessionTests')
foreach($typeName in $typeNames){$type=$assembly.GetType($typeName,$true);$typeTotal=0;$typeFailed=0;foreach($method in $type.GetMethods() | Where-Object { $_.GetCustomAttributes($true) | Where-Object { $_.GetType().FullName -eq 'NUnit.Framework.TestAttribute' -or $_.GetType().FullName -eq 'NUnit.Framework.TestCaseAttribute' } }){$cases=$method.GetCustomAttributes($true)|Where-Object{$_.GetType().FullName -eq 'NUnit.Framework.TestCaseAttribute'};if($cases.Count -eq 0){$cases=@($null)};foreach($case in $cases){$total++;$typeTotal++;try{$args=if($null -eq $case){@()}else{@($case.Arguments)};$method.Invoke([Activator]::CreateInstance($type),$args)}catch{$failed++;$typeFailed++;Write-Output "FAIL $($type.Name).$($method.Name): $($_.Exception.InnerException.Message)"}}};Write-Output "$($type.Name): passed=$($typeTotal-$typeFailed) failed=$typeFailed"}
Write-Output "SUMMARY total=$total passed=$($total-$failed) failed=$failed"
if($failed -gt 0){exit 1}
```

Output (exit 0, no compiler warnings/errors):

```text
ContractValidationTests: passed=25 failed=0
BoardTests: passed=14 failed=0
BoardResolverTests: passed=7 failed=0
CombatTests: passed=15 failed=0
EnemyAndSkillTests: passed=29 failed=0
StageSessionTests: passed=25 failed=0
SUMMARY total=115 passed=115 failed=0
```

### Fix-round self-review

- Explicit threshold triggers are isolated from `EnemyActionData`; a full-HP countdown test proves no trigger effect is executed by the action engine.
- A crossing evaluated by two fresh mechanics, a large jump, and initial-below inputs prove state belongs to the runtime and thresholds are inclusive.
- Source/public-data trigger mutations do not alter runtime behavior; trigger IDs, percent bounds, duplicate IDs, generic enrage semantics, and all action-effect families are boundary-tested.
- No deferred Task 4 minor was addressed.
