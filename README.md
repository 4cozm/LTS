> **License (v1.0.0+)**
> Dual-licensed: **GNU AGPL-3.0-only** (see `LICENSE`) **OR** **Commercial License**
> (see `COMMERCIAL-LICENSE.md`).
>
> v0.9.9 and earlier remain under the previous license (see tag `v0.9.9-last-permissive`).

# 선불권 발급/사용 시스템 (LTS)

레이저 태그 스포츠 경기장에서 사용되는 선불권의 발급, 사용, 환불 및 권한별 관리 기능을 제공하는 내부 운영 시스템.

*   수동 영수증 대조 및 정책 동의 확인 과정에서 발생하는 운영 누수를 방지하고, 모든 상태 변경을 데이터로 추적하기 위해 설계됨.

## 1. 왜 이 프로젝트가 필요한가

*   **운영 가시성 결여**: 직원이 결제 영수증을 확인하고 수동으로 기록하는 과정에서 휴먼 에러 및 정산 불일치가 상시 발생함.
*   **정책 관리 체계 부재**: 선불권 환불 및 유효기간 정책에 대한 고객 동의 증빙 수단이 없어 추후 분쟁 시 운영 리스크가 존재함.
*   **실시간 업무 처리 한계**: 고객의 정책 동의 여부를 직원이 즉각 인지하지 못해 발생하는 불필요한 대기 시간과 운영 지연이 반복됨.

## 2. 핵심 제약

*   **네트워크 화이트리스트**: 카카오톡 알림톡 발송 API 사용을 위해 반드시 고정 IP 서버가 필요함.
*   **통제 및 모니터링 요구사항**: 운영 주체(관리자)의 강력한 시스템 제어 권한과 상세한 작업 이력 감사(Audit Trail) 요구.
*   **비용 및 성능 최적화**: 고정 IP 확보를 위해 무료 티어 인프라(GCP US-East)를 활용하되, 물리적 거리로 인한 지연 시간 극복을 위한 데이터 파싱 속도 최적화 필요.

## 3. 해결 전략

*   **엄격한 상태 전이(State Transition) 모델**: 구매 → 정책 동의 → 완료, 사용 → 인증번호 검증 → 차감으로 이어지는 워크플로우를 정의하여 데이터 원자성을 확보함.
*   **메시징 서비스 격리 (WatchTower)**: 고정 IP 제약 해결을 위해 알림톡 발송 로직을 별도 서비스(WatchTower)로 분리하고, 메인 서버와 비동기 TCP 통신 수행.
*   **바이너리 프로토콜 (Protobuf) 도입**: 서비스 간 메시지 교환 시 JSON 대신 Protobuf를 사용하여 파싱 성능을 극대화하고 백프레셔 리스크를 완화함.
*   **계층형 권한 관리 (RBAC)**: 관리자-매니저-직원으로 이어지는 권한 구조를 통해 기능 접근을 통제하고 하위 계정 관리 기능을 구현함.

## 4. 아키텍처 / 데이터 흐름

```text
[고객] --(정책 동의/인증번호 입력)--> [모바일 웹/카카오톡]
                                      ^
                                      | (SignalR / HTTPS)
                                      v
[직원/매니저] <---(SignalR)--- [LTS Backend (ASP.NET Core)] ---(Protobuf)---> [WatchTower (GCP)]
                               |                                              |
                               +---(Dapper/MySQL)                             +---(Kakao API)
                               +---(Redis / Azure Key Vault)
```

### 통신 프로토콜 상세
*   **LTS ↔ WatchTower**: TCP Socket 기반 통신.
*   **프레임 구조**: 4-byte Length Prefix (Big Endian) + Protobuf Payload.
*   **보안**: HMAC-SHA256 기반의 Challenge-Response 인증을 통해 WatchTower 연결 권한 검증.

## 5. 주요 구현 포인트

*   **동시성 제어 및 원자성**: 
    *   `conn.BeginTransaction()`을 통해 카드 상태 변경과 로그 기록을 단일 트랜잭션으로 처리.
    *   잔액 복구 등 민감한 작업 시 `FOR UPDATE` 구문을 사용한 Row-level Locking으로 데이터 부정합 방지.
*   **임시 상태 관리 (Redis)**: 고객의 정책 동의 대기 상태를 DB가 아닌 Redis에 24시간 TTL(Time-To-Live)로 유지하여 메인 DB의 부하를 줄이고 처리 속도 향상.
*   **SignalR 기반 실시간 UX**: 백엔드의 `ProtoHandler`가 WatchTower로부터 메시지를 수신하는 즉시 SignalR Hub를 통해 프론트엔드로 이벤트를 브로드캐스팅하여 즉각적인 UI 갱신 구현.
*   **직무 기반 접근 제어**: `BasePageModel` 및 `LoginService`를 통한 세션 검증과 페이지별 권한 필터링 적용.

## 6. 운영 관점에서 중요했던 점

*   **인프라 이중화 및 격리**: 카카오톡 발송용 고정 IP 확보를 위해 별도의 GCP 인스턴스를 구성하고 메인 시스템과 결합도를 낮춤.
*   **보안 및 비밀 관리**: Azure Key Vault를 연동하여 API 키, DB 연결 문자열 등 민감 정보를 코드 외부에서 안전하게 관리.
*   **장애 내성**: TCP 연결 단절 시 `TcpClientBackgroundService`에서 1분 간격의 재연결 루프를 실행하여 통신 안정성 확보.

## 7. 트레이드오프와 한계

*   **구조적 복잡성 수용**: 고정 IP 제약으로 인해 별도 인스턴스를 운영하게 됨으로써 배포 및 모니터링 대상이 늘어나는 복잡성을 성능 및 비용 이득과 교환함.
*   **메시지 동기화 오버헤드**: Protobuf 도입으로 런타임 성능은 얻었으나, .proto 파일 관리 및 빌드 타임의 의존성 관리 비용 발생.
*   **최종 일관성 영역**: WebSocket 연결 불안정 시 실시간 알림이 누락될 수 있는 가능성이 존재하며, 이를 보완하기 위한 수동 새로고침 UI 병행.

## 8. 사용 기술

*   **Backend**: C# 9.0 (ASP.NET Core), Dapper
*   **Storage**: MySQL 9.0, Redis (StackExchange.Redis)
*   **Real-time**: SignalR (WebSocket)
*   **Messaging**: Google.Protobuf, Firebase (FCM)
*   **Infra**: GCP Compute Engine (WatchTower), Azure Key Vault
*   **Security**: BCrypt.Net-Next, RBAC Implementation

## 9. 참고 링크

*   **LTS Main Repository**: [https://github.com/4cozm/LTS](https://github.com/4cozm/LTS)
*   **WatchTower (Messaging Service)**: [https://github.com/4cozm/LTS_WatchTower](https://github.com/4cozm/LTS_WatchTower)
