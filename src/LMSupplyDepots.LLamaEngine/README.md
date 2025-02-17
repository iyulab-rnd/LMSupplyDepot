# LMSupplyDepots.LLamaEngine

## 개요
LMSupplyDepots.LLamaEngine은 LLaMA 기반 언어 모델을 로컬 환경에서 관리하고 실행하기 위한 .NET 라이브러리입니다. 모델 로딩, 하드웨어 백엔드 감지(CUDA, Vulkan, CPU), 추론 작업을 처리하며 자원 관리 기능을 내장하고 있습니다.

### 주요 기능
- 하드웨어 백엔드 자동 감지 및 최적화 (CUDA 11/12, Vulkan, CPU)
- 로컬 모델 상태 추적 및 관리
- 효율적인 모델 로딩 및 언로딩
- 스트리밍 방식의 추론 지원
- 텍스트 임베딩 생성
- 자원 정리 및 관리
- 의존성 주입(Dependency Injection) 지원으로 쉬운 통합

## Dependencies
```
LLamaSharp
LLamaSharp.Backend.Cpu
LLamaSharp.Backend.Cuda11
LLamaSharp.Backend.Cuda12
LLamaSharp.Backend.Vulkan
Microsoft.Extensions.DependencyInjection.Abstractions
```