# DemolitionLab 면접 역추적 학습 노트

이 문서는 포트폴리오 설명문이 아니라, AI 도움으로 작성된 코드를 직접 읽고 설명하기 위한 학습 체크리스트다. 문장을 외우기보다 코드에서 근거를 찾고 자신의 말로 다시 설명한다.

## 준비 완료 기준

- [ ] 아래 60초 설명을 원고 없이 말할 수 있다.
- [ ] 핵심 클래스 5개의 역할과 호출 순서를 그릴 수 있다.
- [ ] 디버깅 사례 3개를 `관찰 → 가설 → 측정 → 수정 → 검증` 순서로 설명할 수 있다.
- [ ] README의 모든 수치가 어디에서 나온 값인지 찾을 수 있다.
- [ ] AI가 만든 결과를 수정하거나 폐기한 이유를 구체적으로 말할 수 있다.
- [ ] 모르는 구현은 추측하지 않고 확인 방법을 제시할 수 있다.

## 60초 프로젝트 설명

Demolition Lab은 제한된 포탄으로 구조물의 약점을 찾아 목표 파괴율을 달성하는 5스테이지 Unity 퍼즐이다. 처음에는 Rigidbody만 사용했지만, 연결성 그래프, 무게중심, 국소 지지, 논리 연결과 물리 Joint를 단계적으로 실험했다. 자동 테스트와 실제 플레이가 다른 결과를 내거나, 복잡한 연결이 붕괴를 끈적하게 만드는 문제가 반복됐다. 최종 게임에서는 연구 모델 전체를 사용하지 않고 플레이어가 읽을 수 있는 소수의 Weak/Reinforced FixedJoint만 남겼다. 구현에는 생성형 AI를 적극 사용했으며, 수동 플레이와 런타임 측정을 통해 결과를 수정하거나 폐기하는 방식으로 개발했다.

## 런타임 흐름

```text
Keyboard input
→ LauncherController.Update
→ LauncherController.TryFire
→ Projectile.Launch
→ Projectile.OnCollisionEnter
→ StructuralBlock.HandleProjectileImpact
→ Projectile.MeaningfulImpact event
→ VerticalSliceStageController.HandleImpact

Target movement
→ StageTargetPiece.EvaluateDestroyed
→ VerticalSliceStageController destruction percentage
→ StageFlowStateMachine transition
→ Clear / Failed UI and scene flow
```

## 핵심 코드 1 — LauncherController

파일: [`LauncherController.cs`](../Assets/Scripts/LauncherController.cs)

### 읽어야 할 메서드

- `Awake`: Inspector 참조 검증, 초기 회전 저장, 초기 조준 적용
- `Update`: 키보드 입력, yaw/pitch 제한, 발사, 궤적 갱신
- `TryFire`: Projectile 생성, 속도·질량·수명 전달, `ProjectileFired` 이벤트 발생
- `UpdateAimLine`: 중력만 고려한 포물선 위치 계산

### 반드시 설명할 내용

궤적은 다음 식을 시간 간격마다 계산한다.

`position = start + velocity × time + 0.5 × gravity × time²`

공기 저항과 실제 충돌은 예측하지 않으므로 조준 보조선이지 완전한 미래 경로가 아니다.

### 예상 질문

**왜 발사 후 바로 탄약을 줄이지 않고 이벤트를 사용하는가?**

Launcher는 포탄 생성만 담당하고 스테이지 규칙은 Controller가 담당하도록 책임을 분리했다. 발사가 실제로 성공한 경우에만 `ProjectileFired`를 발생시켜 탄약과 UI를 갱신한다.

**`ValidateRequiredReferences`가 필요한 이유는?**

C# 컴파일 성공은 Unity Inspector의 serialized reference가 연결됐다는 뜻이 아니다. 이전 `projectilePrefab` 누락처럼 Play Mode에서만 나타나는 배선 오류를 시작 시점에 명확한 오류로 바꾸기 위해 사용한다.

## 핵심 코드 2 — Projectile

파일: [`Projectile.cs`](../Assets/Scripts/Projectile.cs)

### 읽어야 할 메서드

- `Launch`: 질량, 초기 선속도, 생성 씬과 플레이어 발사 여부 저장
- `OnCollisionEnter`: 첫 Destructible 충돌만 처리, 접점과 impulse 기록
- `ResetStaticEvents`: Play Mode 재시작 시 static event와 projectile ID 초기화

### 반드시 설명할 내용

Impulse의 단위는 `N·s`이며 운동량 변화량과 같다. 힘 `N`과 impulse `N·s`는 단위가 다르므로 같은 값처럼 직접 더하면 안 된다.

`collision.impulse`는 Unity 물리 해결 과정에서 계산된 충돌 impulse다. 구조물에 전달할 때는 상대 방향을 맞추기 위해 `-collision.impulse`를 사용한다.

### 예상 질문

**왜 첫 충돌만 보고하는가?**

한 발이 여러 접촉을 반복하며 중복 판정과 VFX를 발생시키는 것을 막고, 포트폴리오 게임에서 한 발의 대표 충돌을 일관되게 기록하기 위해서다.

**왜 발사한 씬 이름을 저장하는가?**

씬 전환 직후 남은 이벤트나 잘못된 충돌 피드백이 다음 씬에 적용되지 않도록 현재 씬과 비교하기 위해서다.

## 핵심 코드 3 — StageTargetPiece

파일: [`StageTargetPiece.cs`](../Assets/Scripts/StageTargetPiece.cs)

### 파괴 판정 조건

- 절대 높이가 기준 아래로 내려감
- 초기 위치보다 충분히 낙하함
- 초기 위치에서 충분히 이동함
- 초기 회전에서 충분히 회전함

하나라도 참이면 `IsDestroyed`를 true로 고정한다. 다시 제자리로 돌아와도 파괴율이 감소하지 않는다.

### 예상 질문

**왜 `InitializeReferenceState`와 `BeginEvaluation`을 분리했는가?**

초기 Transform을 저장하는 시점과 파괴 판정을 허용하는 시점을 분리해야 시작 직후의 잘못된 자동 Clear를 막을 수 있기 때문이다.

**왜 `Awake`에서도 초기화하고 Controller에서도 다시 초기화하는가?**

각 Target이 독립적으로 안전한 기본 상태를 갖도록 하면서, Controller가 모든 참조가 준비된 뒤 스테이지 시작 기준을 다시 통일한다. 이 중복이 정말 필요한지는 면접 전에 장단점을 직접 검토한다.

## 핵심 코드 4 — StageFlowStateMachine

파일: [`StageFlowStateMachine.cs`](../Assets/Scripts/StageFlowStateMachine.cs)

허용된 전이는 다음과 같다.

```text
Initializing → Ready → Active → Settling → Clear
                                      └──→ Failed
```

`Transition(expected, next)`가 현재 상태를 검사하므로 잘못된 순서의 호출은 상태를 바꾸지 않는다. 탄약 감소도 `Active` 상태에서만 허용된다.

### 예상 질문

**bool 반환값을 사용하는 이유는?**

호출자가 전이 성공 여부를 확인하고, 이미 Settling이거나 잘못된 상태에서 코루틴이나 후속 동작을 중복 실행하지 않게 하기 위해서다.

## 핵심 코드 5 — VerticalSliceStageController

파일: [`VerticalSliceStageController.cs`](../Assets/Scripts/VerticalSliceStageController.cs)

### 생명주기 순서

1. `Awake`: 상태 초기화, 필수 참조 검증, 입력 비활성화, HUD 준비
2. `OnEnable`: Launcher와 Projectile 이벤트 구독
3. `Start`: Target 기준 상태 초기화, Ready 전환
4. 다음 프레임: Active 전환, Target 평가와 입력 활성화
5. `Update`: 파괴율 갱신, Clear 조건 확인
6. 탄약 소진 또는 Clear: Settling 진입
7. 지연 후 Clear/Failed 확정과 결과 UI 표시

### 예상 질문

**왜 다음 프레임에 Active로 전환하는가?**

씬의 모든 `Awake`와 `Start` 초기화가 끝난 뒤 목표 평가와 입력을 열어 초기 Transform과 Inspector 참조가 안정된 상태에서 시작하기 위해서다.

**왜 Clear를 `clearLockedIn`으로 고정하는가?**

70%에 도달한 뒤 잔해가 움직이거나 판정 조건이 달라져도 성공을 되돌리지 않고, 붕괴 연출을 기다린 뒤 같은 결과를 보여주기 위해서다.

**잠재적인 성능 문제는?**

`DestructionPercentage`가 `CountDestroyedTargets`를 호출하고, 한 프레임에 HUD·Clear 검사·피드백에서 여러 번 순회할 수 있다. 현재 Target 수가 작아 문제는 없지만 규모가 커지면 한 프레임에 한 번 계산해 캐시하거나 이벤트 기반 집계로 바꿀 수 있다.

## Physics와 Visual 분리

파일: [`PhysicsPresentationSeparationBuilder.cs`](../Assets/Editor/PhysicsPresentationSeparationBuilder.cs)

Builder는 Destructible root에 Rigidbody와 Collider를 남기고, MeshFilter와 MeshRenderer를 `VisualRoot`로 이동한다. Validator는 다음을 확인한다.

- PhysicalRoot에 Rigidbody와 Collider가 존재함
- PhysicalRoot에 렌더 geometry가 남지 않음
- VisualRoot에 Renderer와 metadata가 존재함
- VisualRoot 아래에 Rigidbody, Collider, Joint, Target 같은 게임플레이 요소가 없음

외부 메시 교체가 질량, collider 크기, joint anchor와 파괴 판정을 바꾸지 않게 하는 것이 목적이다.

## 대표 디버깅 답변 구조

### Stage02 자가 붕괴

1. 시작 후 약 1초 만에 큰 이동과 회전이 발생했다.
2. 처음에는 Joint 강도나 PhysX 불안정을 의심했다.
3. Joint `4/4`가 살아 있는 상태에서 Transform 변화를 측정했다.
4. deck과 pier 중심이 어긋난 authored geometry 문제를 발견했다.
5. 지지 위치를 이동한 뒤 10초 idle 수치로 다시 검증했다.

핵심은 값을 무작정 강하게 만든 것이 아니라, 살아 있는 Joint와 실제 움직임을 함께 보고 원인 가설을 바꾼 점이다.

### 자동 Clear

1. 발사 전 파괴율이 올라가고 입력이 비활성화됐다.
2. 목표 기준 Transform이 준비되기 전에 평가될 가능성을 확인했다.
3. 초기화와 평가 시작을 분리하고 상태 머신을 도입했다.
4. Active 이전 평가 금지와 최소 한 번의 발사 조건을 추가했다.

### 합성 테스트와 실제 impulse

1. `60 N·s` 자동 테스트는 통과했다.
2. 실제 Weak 충돌은 약 `29.7–35.2 N·s`였다.
3. 테스트 입력이 실제 플레이 분포를 대표하지 못했다.
4. 실제 충돌 로그를 기준으로 capacity를 다시 보정했다.
5. force와 impulse의 단위를 분리한 utilization 형태로 비교했다.

## AI 사용에 대한 정직한 답변

생성형 AI를 코드 초안, Editor 자동화와 문서 작성에 적극적으로 사용했다. 초기에는 생성된 코드를 충분히 읽지 않고 실행 결과 중심으로 진행한 부분도 있었다. 그러나 플레이 결과가 예상과 다르게 나오는 사례가 반복되면서, 문제를 재현하고 로그와 측정값을 확인한 뒤 구현을 수정하거나 폐기하는 방식으로 바뀌었다. 현재는 제출 전에 핵심 런타임 코드를 직접 역추적하며 각 설계의 동작과 한계를 확인하고 있다.

이 답변에서 피해야 할 표현:

- AI가 거의 다 했지만 결과는 좋다.
- 테스트가 통과했으니 코드는 이해하지 않아도 된다.
- 어떤 코드든 AI에게 다시 물어보면 된다.
- 모든 설계와 코드를 처음부터 혼자 작성했다.

## CS·Unity 복습 항목

- [ ] BFS의 queue 동작과 시간복잡도 `O(V + E)`
- [ ] 질량 가중 중심 `Σ(mᵢpᵢ) / Σmᵢ`
- [ ] force `N`, impulse `N·s`, momentum `kg·m/s` 관계
- [ ] Rigidbody mass와 velocity가 운동량·운동에너지에 미치는 영향
- [ ] Unity `Awake`, `OnEnable`, `Start`, `Update`, coroutine 순서
- [ ] C# event 구독 해제와 static event 초기화 이유
- [ ] 상태 머신이 bool flag 여러 개보다 안전한 이유
- [ ] FixedJoint와 ConfigurableJoint의 차이
- [ ] serialized reference와 C# null 검사의 한계
- [ ] 목표물 수가 증가할 때 현재 파괴율 계산의 비용

## 제출 전 남은 사실 확인

- [ ] macOS standalone에서 Title → Stage05 → Title 전체 흐름 QA
- [ ] Stage01–05 Retry와 Next 확인
- [ ] Player log 예외 확인
- [ ] README 수치의 원본 문서와 측정 조건 확인
- [ ] M1–M5 핵심 클래스의 실제 구현 읽기
- [ ] Stage04 성공 장면 재녹화 여부 결정
- [ ] 무음 빌드를 그대로 제출할지 결정

## 2026 넥토리얼 연결

공식 공고는 생성형 AI로 코드를 작성·검증하는 능력, 기존 코드 분석과 디버깅·리팩터링, 개발 생산성 도구와 자동화 경험을 명시한다. DemolitionLab에서는 다음 근거를 사용한다.

| 요구 역량 | 프로젝트 근거 |
| --- | --- |
| C# / Unity | 5스테이지 Vertical Slice와 공용 런타임 코드 |
| 코드 분석·디버깅 | Stage02, 자동 Clear, 실제 impulse 불일치 |
| 리팩터링 | M1–M5 연구 모델에서 핵심 FixedJoint 구조로 축소 |
| 툴·자동화 | Editor scene builder, validator, release preflight |
| AI 작성·검증 | 생성 결과를 수동 플레이와 측정으로 수정·폐기 |
| CS 기본기 | BFS, COM, 단위 분석, 상태 머신 |

팀 개발, 데이터베이스, 네트워크와 멀티스레드 경험은 이 프로젝트의 근거로 주장하지 않는다.
