# DemolitionLab 책임 기반 리팩토링

## 1단계 검증 기록 — 2026-10-17

- 이동한 C# 35개와 기존 .meta 35개: 이동 전 SHA-256과 모두 동일.
- 이동 대상과 링크를 갱신한 두 문서를 제외한 기존 추적 파일 292개: 모두 동일.
- 새 폴더 .meta 14개: 폴더 존재 및 Scripts/Editor 내부 GUID 중복 없음 확인.
- README와 면접 문서의 소스 링크 11개: 새 경로 존재 확인.
- 이동된 스크립트 및 .meta: .gitignore에 의해 무시되지 않음 확인.
- git diff --check 통과. 스테이징·커밋하지 않았고 기존 untracked 에셋은 그대로 둠.
- 정확한 Unity 버전 부재로 컴파일 및 Play Mode는 미실행.
  serialized 필드를 옮기는 2단계의 안전한 완료 판정에는 해당 Unity 검증이 필요하다.

## 목적과 경계

폴더를 기능별로 정리하고, 서로 다른 이유로 변경되는 책임을 분리한다.
클래스 수나 파일 길이를 목표로 삼지 않는다. 상속보다 명시적인 합성을 우선하고,
한 구현만 존재하는 기능에 인터페이스·DI·이벤트 버스를 도입하지 않는다.
단순 제작 작업과 수학 함수는 static 함수로 유지해도 된다.

게임플레이, Rigidbody, Collider, FixedJoint, 목표 판정, 탄약, 성공률, 대기 시간,
포탄 설정, 씬 순서, 연출 수치는 변경하지 않는다. 최종 씬과 M0~M5 연구 씬을 보존한다.

## 진행 상태

- [x] 기존 35개 C# 파일의 역할·함수 구성과 주요 교차 참조 조사
- [x] 1단계: 기능 폴더 이동, 기존 스크립트와 .meta 보존, 문서 링크 갱신
- [x] 2단계 구현: StageHudView와 StageFeedbackController 추출 및 기존 씬 참조 이전
- [x] 3단계 구현: 제작 도구의 UI·연출 배선 변경 및 생성/검증 책임 분리
- [x] 4단계 구현: 연구 코드의 그래프 탐색·진단 책임 분리
- [ ] 각 책임 분리 단계에서 Unity 컴파일, 기존 validator, 수동 회귀 검증

1단계는 파일 위치만 변경했다. 2~4단계 구현과 정적 검증 결과는 아래에 별도로 기록한다.
조사는 함수 구성과 의존성을 기준으로 한 설계 판단이며 전체 알고리즘의 정확성 감사를 뜻하지 않는다.

## 폴더와 의존성

```text
Assets/Scripts/
  Gameplay/Launching/           조준·포탄
  Gameplay/Stage/               상태·목표·스테이지 진행
  Gameplay/Structure/           최종 게임의 지지부 파손 통지
  Presentation/UI/              메뉴와 향후 HUD
  Presentation/Feedback/        충돌·카메라·오디오와 향후 연출 조합
  Presentation/                 시각 분류·촬영 보조
  Utility/                      씬 리셋 보조
  Research/StructuralSimulation/ M1~M5 구조 실험
Assets/Editor/
  Stages/                       최종 스테이지 제작·검증
  Presentation/                 환경·외형·연출 제작·검증
  Release/                      출시 설정·검증·빌드
  Research/                     M0~M5 제작·검증
```

폴더 분류는 어셈블리 격리가 아니다. 기존 namespace와 어셈블리 경계를 유지한다.
Projectile은 여전히 StructuralBlock에 대한 선택적 연구용 충격 전달 경로를 가진다.
CriticalSupportJoint의 컨트롤러 통지와 static 충돌 이벤트도 이번 단계에서는 유지한다.
Editor 코드는 계속 Assets/Editor 아래에 두어 런타임 코드와의 기존 분리를 보존한다.

## 런타임 파일별 판단: 19개

줄 수는 이동 전 기준이며 분리의 합격 기준이 아니다.

| 파일 | 줄 수 | 판단 | 근거와 후속 경계 |
| --- | ---: | --- | --- |
| LauncherController | 203 | 유지 | 입력→조준→발사의 응집된 흐름. 궤적 분리는 독립적인 변경 요구가 생길 때 검토 |
| Projectile | 114 | 유지 | 생성 후 물리 초기화·수명·첫 의미 있는 충돌 보고. 연구 경로는 호환성 때문에 보존 |
| StageFlowStateMachine | 71 | 유지 | Unity 객체와 분리된 상태·탄약 규칙. 상속 계층 불필요 |
| StageTargetPiece | 68 | 유지 | 조각 하나의 기준 상태·파괴 판정·판정 latch 책임 |
| VerticalSliceStageController | 564 | 책임 분리 | 진행 규칙과 HUD, VFX, 오디오, 충돌 연출 제한이 혼재 |
| CriticalSupportJoint | 54 | 유지 | PhysX 파손 콜백→상태·표시·통지. 자체 파손 계산은 하지 않음 |
| PresentationMenuController | 23 | 유지 | 타이틀에서 게임 시작 |
| PresentationAudioHooks | 68 | 유지 | 클립과 재생 소스·음량 관리. 새 오디오 시스템 불필요 |
| SimpleCameraShake | 40 | 유지 | 카메라 흔들림과 원위치 복원 |
| StructuralGroundImpactFeedback | 52 | 유지 | 지면 충돌 선별·쿨다운·이벤트 보고 |
| PresentationCaptureMode | 36 | 유지 | 촬영용 궤적 표시 토글 |
| PresentationVisual | 27 | 유지 | 표시용 분류 데이터. 게임 규칙 추가 금지 |
| ResetController | 32 | 유지 | R 키 리셋과 기존 프로토타입 안내. 사용처 보존 |
| StructuralBlock | 138 | 유지 | 연구 노드의 상태와 물리 전환. 표현 분리는 별도 요구가 있을 때 검토 |
| StructuralConnection | 236 | 내부 정돈 후보 | 논리 용량과 물리 Joint 모드, 진단이 공존. 연구 회귀가 확보되기 전 모드 분리 금지 |
| StructuralStabilityEvaluator | 149 | 유지 | COM·지지 구간이라는 단일 평가. 디버그 기하 데이터 포함만으로 분리하지 않음 |
| LocalSupportEvaluator | 211 | 유지 | 국소 지지 평가와 시각 진단이 한 알고리즘을 설명. 결과 객체는 필요할 때 독립 파일화 |
| StructuralLoadEvaluator | 180 | 유지 | 연결별 하중 평가. 임의의 범용 그래프 프레임워크로 바꾸지 않음 |
| StructureManager | 543 | 후속 책임 분리 | BFS·인접성, 평가 순서 조정, 해제, 로그·기즈모가 혼재. 그래프 탐색 helper와 진단 helper를 우선 후보로 둠 |

## Editor 파일별 판단: 16개

| 파일 | 줄 수 | 판단 | 추출할 책임 / 남길 책임 |
| --- | ---: | --- | --- |
| VerticalSliceSceneBuilder | 798 | 책임 분리 | UI 제작·공통 검증 추출 / Stage01 배치와 제작 순서 유지 |
| VerticalSliceStage02Builder | 500 | 중복 추출 | 공통 조각·Joint 생성과 검증 후보 / 다리 배치와 고유 기준 유지 |
| VerticalSliceStage03Builder | 503 | 중복 추출 | 공통 배선·참조·관통 검사 후보 / 벙커 배치와 강도 유지 |
| VerticalSliceStage04Builder | 541 | 중복 추출 | 공통 생성·검증 후보 / 탑 배치와 지연 설정 유지 |
| VerticalSliceStage05Builder | 770 | 책임 분리·중복 추출 | 장식 생성·공통 검사 후보 / 비대칭 구조 배치와 고유 기준 유지 |
| FinalVisualPolishBuilder | 1051 | 책임 분리 | 조명, 피드백 제작, HUD/타이틀 제작, 검증을 기능별 helper로 분리 / 기존 메뉴 진입점 유지 |
| VerticalSlicePresentationBuilder | 783 | 책임 분리 | 환경·구조 외형·카메라 작업 구분 / 외부 호출 순서 유지 |
| ThirdPartyPresentationBuilder | 729 | 책임 분리 | 소스 에셋 가공·프로젝트 프리팹 제작과 씬 배치·검증 구분 |
| PhysicsPresentationSeparationBuilder | 260 | 유지 | 물리 루트/VisualRoot 이전과 그 불변 조건 검증이 목적상 응집됨 |
| PortfolioReleaseBuilder | 193 | 유지 | 출시 구성·검증·빌드의 작은 진입점. 검증은 기존 도구에 위임 |
| M0SceneBuilder | 244 | 보존 | 기준 프로토타입 제작. 최종 게임 공통화 때문에 연구 씬을 재생성하지 않음 |
| M1SceneBuilder | 336 | 후속 검토 | 연구 씬 제작·검증 분리 후보. 최종 게임 작업 뒤에 진행 |
| M2SceneBuilder | 576 | 후속 책임 분리 | 실험 시나리오·기하 검증과 제작 구분. reflection 호출 대상 보존 |
| M3SceneBuilder | 205 | 보존 | 짧은 연구 시나리오 검증. 공통화의 이익이 작음 |
| M4SceneBuilder | 532 | 후속 책임 분리 | 하중 시나리오 검증과 구조 제작 구분. 실제 측정값 기반 기대값 보존 |
| M5SceneBuilder | 775 | 후속 책임 분리 | 물리 초기화·시뮬레이션 검증과 프레임 제작 구분. 임계값·시뮬레이션 순서 보존 |

공통화는 실제로 같은 작업만 대상으로 한다. 서로 다른 배치·강도·접촉 검증 조건을
하나의 옵션 많은 BuildEverything 함수나 Builder 상속 트리로 합치지 않는다.
우선 기존 메뉴와 public static 메서드를 호환 진입점으로 두고 구체적인 helper에 위임한다.

## 2단계 설계 계약

### VerticalSliceStageController

초기화, 상태 머신, 목표 평가, 탄약, Clear latch, 붕괴 대기, Retry/Next를 소유한다.
발사 및 충돌 이벤트 구독·해제와 승인 조건은 우선 기존 위치에 유지한다.
Awake→OnEnable→Start→다음 프레임 활성화 순서를 변경하지 않는다.

### StageHudView

Text/Image/Button/결과 패널 참조와 표시 함수를 소유한다.
게임 상태를 직접 조회하거나 목표를 평가하지 않는다. 숫자·문자·표시 여부를 전달받는다.
버튼의 기존 컨트롤러 호출을 유지하고 씬 이동 책임을 가져오지 않는다.

### StageFeedbackController

파티클·카메라·오디오, 발사/충돌/Joint 파손/붕괴 연출과 연출용 쿨다운을 소유한다.
탄약·승패·물리 강도를 변경하지 않는다. 승인된 이벤트 데이터와 목표 집계 결과를 전달받는다.
기존 충격량 필터, 붕괴 집계 창, 중복 연출 억제 수치를 보존한다.
첫 추출 시 새로운 전역 이벤트나 중개 계층을 추가하지 않는다.

### 참조 이전과 제작 도구

새 컴포넌트 추가만으로 기존 serialized 필드는 이전되지 않는다.
Stage01~05 각각에서 UI와 피드백 참조를 명시적으로 이전한 뒤 기존 필드를 제거한다.
FindProperty("stageLabelText"), FindProperty("impactDust"), FindProperty("audioHooks") 등의
사용처를 제작·검증 도구에서 새 참조 경로로 바꾼다. 기존 씬뿐 아니라 재제작 경로도 검증한다.
리플렉션으로 연구 코드의 private 메서드를 호출하는 검증도 메서드 추출 시 함께 갱신한다.

파괴율 getter가 현재 목표 판정을 갱신한다는 점도 유의한다.
표현 분리 과정에서 평가 횟수·시점을 임의로 바꾸거나 별도 캐시를 도입하지 않는다.

## 검증과 실행 제약

1단계에서는 이동 전후 35개 C# 및 35개 .meta의 바이트 동일성을 비교한다.
기존 나머지 추적 파일은 문서 링크 수정 외에 바뀌지 않았는지 검사한다.
새 폴더 .meta의 GUID 중복, 문서 링크, git diff 공백 오류도 확인한다.

내용 분리 후 Unity에서는 다음을 실행한다.

1. 프로젝트 버전 6000.5.10f1로 열어 컴파일 완료 및 Missing Script 유무 확인.
2. DemolitionLab/Presentation/Validate Final Portfolio Build 실행.
3. DemolitionLab/Release/Validate Release Candidate 실행.
4. Title→Stage01~05: 조준, 단발 발사/탄약, 파괴율, Clear/Failed, Retry/Next/최종 복귀 확인.
5. 10초 무입력 시 자동 Clear 없음, 결과 상태 발사 차단, 충돌 연출 중복 없음 확인.

검증기는 씬을 열거나 임시 객체·시뮬레이션을 사용할 수 있으므로 저장하지 않은 작업을
먼저 정리한다. 검증 목적으로 Build/Apply 메뉴를 실행해 최종 씬을 덮어쓰지 않는다.

현재 머신의 Unity CLI 설치 목록에는 6000.5.10f1이 없었다.
다른 버전으로 원본 프로젝트를 열어 업그레이드하지 않는다.
따라서 이 기록의 1단계 정적 검증을 Unity 컴파일·Play Mode 성공으로 해석하면 안 된다.

## 2~4단계 구현 기록 — 2026-10-17

### 책임 분리

- VerticalSliceStageController: 564→417줄. 초기화·상태·목표 집계·승패·씬 이동을 유지.
- StageHudView: 75줄. UI 참조와 표시만 소유하며 목표 판정이나 승패를 변경하지 않음.
- StageFeedbackController: 209줄. 연출 참조, 연출 쿨다운과 기존 연출 수치를 소유.
- Stage01~05: 같은 컨트롤러 객체에 두 컴포넌트를 명시적으로 추가하고 표시 참조 85개 이전.
- Stage01~05의 생성 도구: 새 컴포넌트에 배선. 검증 함수는 Stage01Validation~Stage05Validation으로 이동.
- StageHudFactory: 초기 HUD 생성. StagePresentationValidation: 컴포넌트 소유 객체와 내부 필수 참조 검사.
- FinalVisualPolishBuilder: 1,051→141줄. 기존 메뉴·외부 호출 진입점은 유지하고 조명, 충돌 연출,
  HUD, 타이틀 제작은 FinalLightingBuilder, FinalImpactFeedbackBuilder,
  FinalStageHudBuilder, FinalTitleSceneBuilder에 위임.
- ThirdPartyPresentationBuilder: 729→298줄. 원본 에셋 가공·프리팹 제작은 IndustrialAssetFactory로 추출.
- VerticalSlicePresentationBuilder: 783→650줄. 검사 책임은 IndustrialPresentationValidation으로 추출.
- StructureManager: 543→342줄. BFS와 anchor 판정은 StructuralGraphTraversal,
  로그·관통 진단·기즈모는 StructuralDiagnostics로 추출.
- 연구 validator가 reflection으로 사용하는 StructureManager.Awake와 FindActiveComponents는 유지.

새 컴포넌트는 기존 컨트롤러와 같은 GameObject에 둔다. 생성 도구에서는 GetComponent로
각 컴포넌트의 SerializedObject를 얻고 해당 소유자의 필드를 수정한다.
이름만 받아 아무 객체에서나 필드를 찾는 범용 라우터나 새 이벤트 버스는 추가하지 않았다.
상속 계층·DI·풀링·새 물리 시스템·어셈블리 분할도 도입하지 않았다.

파괴율을 읽는 시점, 초기화 순서, 이벤트 구독/해제, 발사 승인 필터와 탄약 처리 순서를 유지했다.
Clear/Failed 대기 시간과 마지막 씬의 Retry 동작도 변경하지 않았다.
대형 연구 씬 Builder와 StructuralConnection의 추가 내부 분리는 이번 완료 범위가 아니다.
아래 초기 파일별 표는 리팩토링 전의 진단이며 모든 후보를 무조건 분리한다는 뜻은 아니다.

### 실행한 검증

재실행 방법은 [Tools/README.md](../Tools/README.md)에 있다.

- Python 정적 회귀: Stage01~05 기존 객체 블록 3,912개 동일, 새 표시 컴포넌트만 추가.
- HUD/연출 참조 85개: 이전 값, 유효한 fileID, 소유 GameObject 모두 확인.
- 기존 스크립트 .meta 35개: 바이트 동일. 새 스크립트/폴더 GUID 중복 없음.
- 프리팹·프로젝트 설정·패키지·연구 씬·타이틀 등 보호 파일 51개: 원본과 동일.
- 발사기·포탄·상태 머신·목표 판정·Joint 콜백 소스: 원본과 동일.
- 초기화·이벤트·진행·연구 규칙 메서드 18개: 공백을 제외한 원본과 동일.
- Roslyn: Assets 내 자체 C# 54개 구문 검사 통과.
- 상태 머신: 실제 소스를 별도 .NET 실행기에 링크한 탄약/상태 테스트 61개 assertion 통과.
- 로컬 클래스 선언·정적 호출 존재/접근성/인수 개수 검사 통과.
- git diff --check 통과. 생성된 .NET bin/obj는 Tools/.gitignore에서 제외.

### 남은 엔진 검증

현재 Unity CLI의 실제 설치 목록은 비어 있다. 사용 가능한 릴리스 목록을 설치 목록으로
해석하지 않았으며 다른 버전으로 프로젝트를 열거나 자동 업그레이드하지 않았다.
정확한 버전을 지정해 기존 최종 validator 실행도 시도했지만 아래 명령은
`Editor 6000.5.10f1 (Apple Silicon) is not installed`로 종료했다(exit code 6).

```sh
Unity run . --editor-version 6000.5.10f1 --non-interactive --json -- -batchmode -nographics -quit -executeMethod FinalVisualPolishBuilder.ValidateFinalPortfolioBuild
```

따라서 Unity 전체 C# 컴파일, 기존 Unity validator, Play Mode 및 standalone은 실행하지 못했다.
Roslyn 검사는 Unity API의 타입 적합성이나 PhysX 결과를 증명하지 않는다.
위 Unity 메뉴 검증과 수동 회귀가 끝나기 전에는 전체 회귀 완료 또는 플레이 품질 보장을 주장하지 않는다.

구현·검증 단계에서는 Git에 반영하지 않았다. 이후 사용자의 별도 승인에 따라
리팩토링 관련 변경만 vertical-slice 브랜치에 커밋·push한다.
기존 untracked 외부 에셋과 개인 자료는 변경하거나 포함하지 않는다.

## 브랜치 통합

main의 사용자 README 수정과 vertical-slice의 리팩토링 변경을 병합한다.
사용자가 수정한 설명 문구는 보존하고, 코드 경로·폴더 구조·검증 상태는 현재 구현에 맞춘다.
배포·문서 기준은 main으로 통일하며 통합 시점의 두 브랜치는 동일한 커밋을 가리킨다.
Unity 실행 검증이 남아 있다는 상태는 병합으로 달라지지 않는다.
