# 리팩토링 검증

저장소 루트에서 실행한다. 아래 검사는 Unity를 실행하지 않으며 에셋을 수정하지 않는다.

```sh
python3 Tools/verify_refactoring.py
dotnet run --project Tools/RefactoringChecks/RefactoringChecks.csproj
```

첫 명령은 Python 3와 Git이 필요하다. 기본 비교 기준은 리팩토링 전 커밋
`d934eb4d433b1e8fd9d943783d6b0b5489c76b7e`이며 `--baseline <commit>`으로 지정할 수 있다.
이 검사는 현재 리팩토링의 동작 보존을 검증하는 도구다. 이후 의도적으로 게임 설정을
바꾼다면 기준과 기대값도 검토해야 한다.

- Stage01~05 기존 객체 블록, 물리 설정, 스테이지 규칙·목표 목록 보존
- HUD/연출 참조 85개 이전, 새 컴포넌트 소유 객체와 fileID 연결
- 기존 스크립트 .meta 35개와 GUID 보존
- 프리팹·패키지·프로젝트 설정·연구 씬·타이틀 보존
- 주요 런타임 파일과 초기화·이벤트·규칙 메서드 보존

두 번째 명령은 .NET 10 SDK가 필요하며 외부 NuGet 패키지를 추가하지 않는다.
SDK에 포함된 Roslyn으로 Assets/Scripts와 Assets/Editor의 C# 구문을 검사하고,
로컬 클래스의 명시적 정적 메서드 호출에서 존재 여부·접근성·인수 개수를 검사한다.
실제 StageFlowStateMachine.cs를 링크해 탄약과 상태 전환 테스트도 실행한다.
빌드 산출물은 Tools/.gitignore에서 제외한다.

**이 도구들은 Unity 전체 컴파일이나 물리 실행 검증을 대체하지 않는다.**

Unity 6000.5.10f1에서 컴파일 후 다음 메뉴를 실행한다.

1. DemolitionLab/Presentation/Validate Final Portfolio Build
2. DemolitionLab/Release/Validate Release Candidate

검증기는 씬을 열거나 물리를 시뮬레이션할 수 있으므로 저장하지 않은 작업을 먼저 정리한다.
씬 재생성이 필요한 경우에만 별도로 Build/Apply 메뉴를 사용한다.

마지막으로 Title→Stage01~05에서 10초 무입력, 조준·발사·탄약, 파괴율,
성공·실패, 결과 상태 입력 차단, Retry·Next·타이틀 복귀와 연출을 직접 확인한다.
