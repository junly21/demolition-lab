# Demolition Lab 포트폴리오 사실 기록

README와 포트폴리오 PDF에서 사용할 수치와 구현 범위를 한곳에 정리한 문서입니다. 확인되지 않은 성능이나 플랫폼 지원은 포함하지 않습니다.

## 프로젝트 기본 정보

- Unity 버전: `6000.5.10f1` (Unity 6.5)
- 렌더 파이프라인: Universal Render Pipeline (URP)
- 언어: C#
- 제품명: `Demolition Lab`
- 버전: `1.0.0-portfolio`
- 플레이 가능한 스테이지: Title 화면 이후 5개
- GitHub 기본 브랜치: `main`
- Release candidate 기준 커밋: `9d85f0e chore: prepare portfolio release candidate`

## 게임 진행

플레이어는 발사대의 각도를 조절하고 제한된 포탄을 사용해 구조물의 Weak 지점을 노립니다. Rigidbody 포탄이 충돌하면 구조물이 물리적으로 무너지고, 설정된 파괴율을 달성하면 스테이지를 통과합니다.

실패한 스테이지는 Retry로 다시 시작할 수 있습니다. Stage01–04를 통과하면 Next가 나타나고, Stage05를 완료하면 Title 화면으로 돌아갈 수 있습니다.

## 최종 물리 구조

- 구조물의 `PhysicalRoot`에는 Rigidbody, BoxCollider, 게임플레이에 필요한 FixedJoint와 선택적인 `StageTargetPiece`가 있습니다.
- 교체 가능한 `VisualRoot`에는 Renderer 기반 외형만 두며 질량, Collider 크기, Joint anchor와 목표 판정에는 관여하지 않습니다.
- 포탄은 Rigidbody 초기 속도로 발사되며 Unity가 계산한 충돌 접점과 impulse를 기록합니다.
- 최종 스테이지는 일반 Rigidbody 충돌과 소수의 Weak/Reinforced FixedJoint를 사용합니다.
- M1–M5에서 만든 그래프, COM, 하중 계산 시스템은 최종 스테이지에서 실행하지 않습니다.
- 목표물은 기준 Transform을 명시적으로 초기화한 뒤 이동, 낙하, 회전 또는 제거 조건으로 파괴 여부를 판단합니다.

## 스테이지별 설정

- **Stage01:** 목표물 8개, Weak Joint 2개, Reinforced Joint 2개, 탄약 3발
- **Stage02:** 독립적으로 지지되는 두 구간의 긴 교량, 탄약 3발
- **Stage03:** 중앙보다 양쪽 Weak 지지부를 노리는 낮은 벙커, 탄약 4발
- **Stage04:** 하부 지지 경로 3개, 목표물 21개, 탄약 4발, Clear 이후 추가 붕괴 관람 시간 2초
- **Stage05:** 5개 목표물로 구성된 구간 3개, Weak Joint 6개, Reinforced Joint 4개, 탄약 4발
- 모든 스테이지의 성공 기준은 70%이며 Stage05는 목표물 15개 중 11개가 파괴되어야 합니다.

## 구조 모델 실험 과정

- **M0:** 발사대, 포탄 수명, 충돌 반응과 씬 재시작을 갖춘 Rigidbody 철거 프로토타입
- **M1:** 직접 구성한 구조 그래프와 바닥 anchor에서 시작하는 BFS 연결성 검사
- **M2/M2.1:** 구조 묶음의 무게중심, 밀도에 맞춘 질량, Collider 간격 검사와 물리 전환 정리
- **M3:** 수직 방향의 국소 지지와 제한된 cantilever 판정
- **M4/M4.1:** 끊어질 수 있는 논리 연결과 중력·충격 utilization 정규화
- **M5/M5.1:** 물리 Joint의 힘 전달, stiffness와 파괴 기준 보정, 논리·물리 연결의 파괴 상태 동기화
- **Final:** 실험에서 얻은 판단은 유지하되, 실제 게임은 Rigidbody, Collider와 FixedJoint 중심으로 단순화

## 측정으로 확인한 디버깅 사례

- M2.1에서 기존 `20:1`이던 바닥과 블록의 질량 차이를 제거했습니다. 단위 블록은 `1 kg`, 부피가 세 배인 바닥은 `3 kg`으로 맞췄습니다.
- M4의 일반적인 Weak 직격 impulse는 약 `29.7–35.2 N·s`였습니다. 기존 자동 테스트의 `60 N·s` 입력은 실제 플레이를 대표하지 못했습니다.
- M4.1에서는 단위가 다른 newton과 newton-second를 직접 더하지 않고 `gravityLoad / gravityCapacity`, `impactImpulse / impactCapacity`로 나눠 비교했습니다.
- M5.1 수동 측정값은 최대 `17.55 N·s`, 중앙값 `5.5 N·s`였습니다. 유효한 직격은 약 `12–17.5 N·s`였으며, 이를 기준으로 물리 Joint의 파괴 기준을 낮췄습니다. 기본 포탄의 `2 kg`, `18 m/s` 설정은 유지했습니다.
- Stage04 포탄은 multiplier 적용 후 `2.3 kg`, `19.8 m/s`입니다. 최종 breakable probe에서는 `19.82 m/s`, `19.60 N·s`가 측정됐습니다.

## 외부 에셋

- 최종 환경과 소품: Kenney City Kit (Industrial), Kenney Factory Kit — CC0
- 바닥 머티리얼: Poly Haven Asphalt 01 — CC0
- Poly Haven Concrete는 가져왔지만 최종 구조물에는 사용하지 않습니다.
- 전체 출처와 수정 기록: [`third-party-assets.md`](third-party-assets.md)
- 현재 외부 오디오 에셋은 사용하지 않습니다.

## 근거 문서

- 스테이지 문서: `vertical-slice-stage01.md`부터 `vertical-slice-stage05.md`
- 구조 모델 실험: `m1-structural-support.md`, `m2-structural-stability.md`, `m3-local-support.md`, `m4-breakable-connections.md`, `m5-physical-connections.md`
- 외부 에셋 기록: [`third-party-assets.md`](third-party-assets.md)
- Release QA: [`release-qa.md`](release-qa.md)
