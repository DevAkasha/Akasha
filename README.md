#Akasha Framework 기술 문서
## 1\. 아키텍처 개요

### 핵심 설계 원칙

Akasha Framework는 **애그리게이트 중심 아키텍처**를 채택한 Unity 게임 개발 프레임워크입니다. 전통적인 컴포넌트 시스템의 복잡성을 해결하기 위해 DDD의 애그리게이트 패턴을 게임 개발에 최적화했습니다.

### 문제 해결 접근법

-   **절차형 프로그래밍**: 단순한 제어흐름 ↔ 캡슐화/추상화 부족
-   **객체지향 프로그래밍**: 우수한 캡슐화 ↔ 분산된 제어흐름과 복잡한 클래스 관계
-   **애그리게이트 패턴**: 경계 내 강결합 허용 + 경계 간 약결합 유지

### 기술적 특징

```
애그리게이트 = 동일한 라이프사이클 + 공유 모델을 가진 클래스 집합
AggregateRoot = 라이프사이클 제어 + 외부 접점 역할
```

**애그리게이트 경계 내부**: 직접 참조, 강결합 허용 **애그리게이트 경계 외부**: 매니저를 통한 간접 참조, 약결합

### 제공 가치

1.  **통제된 복잡성**: 애그리게이트 단위 캡슐화로 인지 부하 감소
2.  **데이터 중심 설계**: 모델 공유를 통한 일관된 상태 관리
3.  **확장성**: 기능 객체(Part-게임객체, View-UI객체)의 자유로운 추가/제거

## 2\. 애그리게이트 구조

### 게임객체 애그리게이트

```
Controller (단독형)
├─ 로직 + 기능 + 상태 전부 담당
└─ 제한된 RxVar만 소유

MController + Model (모델 소유형)
├─ MController: 로직 + 기능 담당
└─ Model: 모든 반응형 프로퍼티 소유

EMController + Entity + Model (확장형)
├─ EMController: 로직 담당
├─ Entity: 기능 담당 + 모델 소유
├─ Model: 모든 반응형 프로퍼티 소유
└─ Part[]: 추가 기능 담당 (다수 가능)
```

### UI객체 애그리게이트

```
Presenter + View[]
├─ Presenter: UI 로직 담당
├─ View[]: UI 기능 + 표현 담당
└─ ViewModel[]: 모델 래핑 (View당 다수 가능)
```

### 구성 규칙

-   **Model**: BaseModel 상속, 반응형 프로퍼티 컨테이너
-   **Entity/Part**: 반응형 프로퍼티 소유 불가, 모델 참조만 가능
-   **View**: 제한된 반응형 프로퍼티 + ViewModel 소유 가능
-   **ViewModel**: 특정 Model 타입 래핑, UI 특화 데이터 변환

### 라이프사이클 공유

동일 애그리게이트 내 모든 구성요소는 AggregateRoot의 라이프사이클을 따라갑니다. 생성, 초기화, 활성화, 비활성화, 소멸이 동기화되어 실행됩니다.

## 3\. 클래스 관계 정의

### 권한 체계

```
강한관계: Read + Write + Call
약한관계: Read + Call
매우약한관계: Read Only
```

### 소유 체계

```
소유관계: Create + Store(호출점,원천) + Destroy + 라이프사이클 공유
관리관계: Store(호출점,원천) + 라이프사이클 비공유 (외부 생성/소멸)
참조관계: 외부 객체 접근만 (생성/보관/소멸 권한 없음)
일시적소유: Create + Destroy (보관하지 않음)
```

### 핵심 관계 맵

```
ContainerManager ──약한관리──→ AggregateRoot
EMController ──강한관리──→ Entity ──강한관리──→ Part[]
Entity ──강한소유──→ Model ←──강한참조── Part
MController ──강한소유──→ Model
Presenter ──강한관리──→ View[] ──약한소유──→ ViewModel[]
ViewModel ──매우약한참조──→ Model
```

### 접근 패턴

-   **애그리게이트 내부**: 직접 참조 허용 (this.Model, this.Entity)
-   **애그리게이트 외부**: 매니저 경유 (GameManager.Controllers.Get<>())
-   **모델 접근**: 인터페이스 기반 (IModelOwner.GetBaseModel())

## 4\. 반응형 시스템

### 기본 반응형 타입

```
RxVar<T>: 기본 반응형 변수
├─ Set(value): 값 변경
├─ AddListener(callback): 변경 구독
└─ Value: 현재 값 읽기

RxMod<T>: 수정자 적용 반응형 변수
├─ SetModifier(key, type, value): 수정자 추가
├─ RemoveModifier(key): 수정자 제거
└─ Value: 계산된 최종 값
```

### 수정자 시스템

```
계산 순서: (원본 + OriginAdd) × (1 + AddMultiplier) × Multiplier + FinalAdd

ModifierType:
├─ OriginAdd: 원본값에 더함
├─ AddMultiplier: 곱셈 계수에 더함 (백분율)
├─ Multiplier: 직접 곱함
└─ FinalAdd: 최종 결과에 더함
```

### 상태 기계

```
FSM<StateEnum>: 유한 상태 기계
├─ Request(state): 상태 전이 요청
├─ AddTransitionRule(to, rule): 전이 규칙 추가
├─ SetPriority(state, priority): 우선순위 설정
└─ RequestByPriority(candidates): 우선순위 기반 전이

RxStateFlagSet<FlagEnum>: 플래그 집합
├─ SetValue(flag, bool): 플래그 설정
├─ AnyActive(flags): OR 조건 검사
└─ AllSatisfied(flags): AND 조건 검사
```

### 효과 시스템

```
ModifierEffect: 시간 제한 수정자
├─ ApplyMode: Manual/Timed/Passive
├─ Duration: 지속 시간
├─ StackBehavior: Stack/Replace/KeepFirst/Max/Min
└─ Interpolator: 보간 함수 (선택적)

DirectEffect: 직접 값 변경
ComplexEffect: 복합 효과 (여러 효과 조합)
```

## 5\. 생명주기 관리

### 표준 생명주기

```
Awake → ModelReady → Init → Start → LateStart
  ↓
Enable ↔ Disable (게임 실행 중 반복)
  ↓
Deinit → Destroy
```

### 풀링 생명주기

```
풀에서 꺼낼 때: OnSpawnFromPool → PoolInit
풀에 반환할 때: PoolDeinit → OnReturnToPool
```

### 타입별 생명주기 특화

```
Controller: 단일 객체 생명주기
MController: Model과 동기화
EMController: Entity → Part[] 순차 호출, Part[] → Entity → Controller순으로 실행
Presenter: View[] 관리 + Show/Hide 추가
```

### 라이프사이클 제어권

-   **AggregateRoot**: 애그리게이트 전체 생명주기 조율
-   **ContainerManager**: AggregateRoot 생명주기 관리
-   **GameManager**: 전체 시스템 생명주기 조율

### 자동 정리

씬 언로드, 애플리케이션 종료 시 등록된 모든 객체의 자동 정리가 수행됩니다. RxBase 상속 객체들의 리스너 해제, 타이머 취소, 효과 제거 등이 포함됩니다.

## 6\. 객체 관리 시스템

### 컨테이너 아키텍처

```
GameManager
├─ ControllerManager: BaseController 계열 관리
└─ PresenterManager: BasePresenter 계열 관리

ContainerManager<T>
├─ 등록된 객체 추적: Dictionary<int, T>
├─ 활성 객체 관리: Dictionary<int, T>
└─ 풀 관리: Dictionary<Type, Queue<T>>
```

### 풀링 구성

```
PoolConfig<T>
├─ preloadCount: 미리 생성할 개수
├─ maxPoolSize: 최대 풀 크기
├─ autoReturn: 씬 전환 시 자동 반환
└─ prefab: 원본 프리팹

스택 동작:
├─ Stack: 중첩 허용
├─ ReplaceLatest: 최신 값으로 교체
├─ KeepFirst: 첫 번째 값 유지
├─ TakeMaximum: 최댓값 선택
└─ TakeMinimum: 최솟값 선택
```

### 생성 패턴

```
// 풀에서 가져오기
var obj = GameManager.Controllers.Spawn<TestController>();

// 프리팹에서 생성
var obj = GameManager.Controllers.SpawnFromPrefab(prefab);

// 풀 우선, 없으면 생성
var obj = GameManager.Controllers.SpawnOrCreate(prefab);

// 풀에 반환
GameManager.Controllers.ReturnToPool(obj);
```

### 메모리 최적화

-   **미리 워밍업**: PrewarmPool로 성능 향상
-   **선택적 정리**: ClearPool로 메모리 절약
-   **자동 정리**: 씬 전환 시 씬별 객체 자동 처리
-   **통계 모니터링**: 실시간 풀 사용량 추적

## 7\. UI 바인딩 시스템

### MVVM 구조

```
Presenter (Controller)
├─ UI 로직 처리
└─ View[] 생명주기 관리

View (View)
├─ UI 표현 담당
├─ 컴포넌트 생성/관리
└─ ViewModel[] 소유

ViewModel (Model)
├─ 특정 Model 래핑
└─ UI 특화 데이터 변환
```

### 데이터 바인딩

```
ViewSlot 시스템:
├─ RxVarSlot<T>: RxVar과 연결
├─ RxModSlot: RxMod와 연결
├─ FSMSlot<T>: FSM과 연결
└─ FlagSetSlot<T>: RxStateFlagSet과 연결

자동 동기화:
Model.health.Set(50) → RxModSlot 감지 → UI 업데이트
```

### 바인딩 설정

```
public class HealthBarView : BaseView
{
    private RxModIntSlot healthSlot;
    
    protected override void SetupComponents()
    {
        healthSlot = ViewSlotFactory.CreateRxModInt("health");
        healthSlot.AddValueChangeListener(OnHealthChanged);
    }
    
    private void OnHealthChanged(object value)
    {
        // UI 업데이트 로직
        healthBar.fillAmount = (int)value / 100f;
    }
}
```

### ViewModel 패턴

```
public class UnitViewModel : BaseViewModel<UnitModel>
{
    protected override void OnTypedModelBound(UnitModel model)
    {
        // 모델 구독 설정
        model.health.AddListener(OnHealthChanged);
        model.level.AddListener(OnLevelChanged);
    }
    
    // UI 특화 변환 로직
    public string GetHealthText() => $"{model.health.Value}/{model.maxHealth.Value}";
    public float GetHealthPercent() => (float)model.health.Value / model.maxHealth.Value;
}
```

## 8\. 확장 시스템

### 비헤이비어 트리

```
BehaviorTree 구성:
├─ Selector: OR 논리 (하나 성공까지)
├─ Sequence: AND 논리 (하나 실패까지)
├─ Inverter: 결과 반전
├─ Condition: 조건 검사
└─ BehaviorAction: 실행 액션

FSM 연동:
├─ FSMStateCondition: 특정 상태 검사
├─ SetFSMStateAction: 상태 변경
├─ FlagCondition: 플래그 검사
└─ SetFlagAction: 플래그 설정
```

### 타이머 시스템

```
UnityTimer 특징:
├─ MonoBehaviour 독립적
├─ 프레임 기반 업데이트
└─ TimerHandle로 취소 가능

사용법:
this.DelayedCall(1f, callback);    // 1초 후 실행
this.RepeatingCall(0.5f, callback); // 0.5초마다 실행
handle.Cancel();                     // 타이머 취소
```

### 디버깅 도구

```
디버그 확장:
├─ RxVar.WithDebug("라벨"): 값 변경 로그
├─ FSM.WithDebug(): 상태 전이 로그
└─ RxStateFlagSet.WithDebug(): 플래그 변경 로그

매니저 통계:
├─ GetSystemStatistics(): 전체 시스템 현황
├─ GetInstanceStatistics(): 인스턴스별 통계
└─ Unity Inspector: 실시간 상태 확인
```

### 세이브/로드

```
Model 기반 자동 직렬화:
├─ GetSaveData(): JSON 문자열 생성
├─ LoadSaveData(json): JSON에서 복원
└─ 필드명 기반 자동 매핑

확장 가능한 설계:
├─ 커스텀 매니저 추가
├─ 새로운 반응형 타입 정의
└─ 애그리게이트 타입 확장
```

### 모듈성

각 기능은 독립적으로 사용 가능하며, 프로젝트 요구사항에 맞게 선택적 활용이 가능합니다. 매니저 우선순위 조정을 통한 의존성 관리와 런타임 기능 추가/제거가 지원됩니다.
