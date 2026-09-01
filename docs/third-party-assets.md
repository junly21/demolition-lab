# 외부 에셋 사용 기록

이 문서는 Demolition Lab에 사용한 외부 에셋의 출처, 라이선스와 수정 내용을 기록합니다. 게임플레이 코드와 물리 구조는 프로젝트에서 작성했으며, 아래 에셋은 환경과 장식 표현에만 사용했습니다.

## 최종 게임에 사용한 에셋

### Kenney City Kit (Industrial)

- **에셋명:** City Kit (Industrial) 1.0
- **제작·배포:** Kenney
- **출처:** [kenney.nl/assets/city-kit-industrial](https://kenney.nl/assets/city-kit-industrial)
- **라이선스:** Creative Commons Zero (CC0 1.0)
- **사용처:** 원경의 산업용 창고, 공장 실루엣, 냉각 굴뚝과 저장 탱크
- **수정 내용:** 프로젝트 전용 visual prefab으로 감싸고 URP 머티리얼, 크기와 배치를 조정했습니다.

### Kenney Factory Kit

- **에셋명:** Factory Kit 3.0
- **제작·배포:** Kenney
- **출처:** [kenney.nl/assets/factory-kit](https://kenney.nl/assets/factory-kit)
- **라이선스:** Creative Commons Zero (CC0 1.0)
- **사용처:** 화물 상자, 공장 기계, 배관, 야드 크레인과 발사대 외형
- **수정 내용:** 프로젝트 전용 visual prefab으로 감쌌습니다. Renderer 이외의 컴포넌트는 제거하고 URP 머티리얼을 적용했습니다.

### Poly Haven Asphalt 01

- **에셋명:** Asphalt 01
- **제작·배포:** Poly Haven
- **출처:** [polyhaven.com/a/asphalt_01](https://polyhaven.com/a/asphalt_01)
- **라이선스:** Creative Commons Zero (CC0)
- **사용처:** 철거장 바닥 머티리얼
- **수정 내용:** 런타임 최대 크기를 2048로 설정하고 roughness 맵을 URP metallic/smoothness mask로 변환했습니다.

## 가져왔지만 최종 게임에는 사용하지 않은 에셋

### Poly Haven Concrete

- **에셋명:** Concrete
- **제작·배포:** Poly Haven
- **출처:** [polyhaven.com/a/concrete](https://polyhaven.com/a/concrete)
- **라이선스:** Creative Commons Zero (CC0)
- **제외 이유:** 플레이 화면에서 표면 방향이 나무결처럼 보여 구조물 콘크리트에 어울리지 않았습니다.
- **현재 상태:** 원본 파일만 보관하고 있으며, 최종 구조물에는 프로젝트에서 만든 얼룩 패턴 텍스처를 사용합니다.

## 적용 원칙

1. 외부 원본은 가능한 한 `Assets/Art/ThirdParty/<AssetName>` 아래에 보관합니다.
2. 씬에 사용하기 전에 이 문서에 출처와 라이선스를 기록합니다.
3. 원본 파일을 직접 수정하지 않고 `Assets/Art/Prefabs` 아래에 프로젝트용 prefab을 만듭니다.
4. 외형 prefab에는 Rigidbody, Collider, Joint와 게임플레이 컴포넌트를 넣지 않습니다.
5. 구조물의 `PhysicalRoot`는 바꾸지 않고 `VisualRoot` 아래의 외형만 교체합니다.
6. `VisualRoot`의 local position과 rotation은 0, scale은 1로 유지하고 실제 크기 조정은 그 아래 자식에서 처리합니다.
7. 에셋을 교체한 뒤 presentation validator와 vertical-slice validator를 실행합니다.
