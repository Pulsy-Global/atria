<div align="center">
  <img src="https://cdn.pulsy.app/atria/oss/banner.png" alt="Atria Banner" width="100%" />
  <br/>
  <p>
    <a href="https://pulsy.app/atria">Website</a> •
    <a href="https://docs.pulsy.app/atria/getting-started/overview">Documentation</a>
  </p>
  <p>
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4" alt=".NET 10.0">
    <img src="https://img.shields.io/badge/-Docker-2496ED?logo=docker&logoColor=white" alt="Docker">
    <img src="https://img.shields.io/badge/License-BSL%201.1-lightgrey" alt="License">
  </p>
</div>

## Overview

Atria is Pulsy's off-chain backend for event-driven blockchain workflows. It helps teams process on-chain data, apply custom logic, and trigger real-time actions across their systems.

- Build feeds that read blockchain data and apply custom logic.
- Connect feeds to external systems in real time.
- Use AI-assisted feed creation and open-source Atria Library.
- Run through Atria Cloud, self-managed, private, or on-prem deployments.

## Quick Start
Run Atria locally using Docker:
```bash
curl -fsSL https://raw.githubusercontent.com/Pulsy-Global/atria/main/deploy/docker/install.sh | bash
cd ./atria-oss/prod
docker compose up -d
```
> You can configure the environment via the generated `.env` file.

## Documentation
Getting started guides, runtime concepts, deployment options, and the Atria Library are available on our **[documentation site](https://docs.pulsy.app/atria/getting-started/overview)**.

## Development

See [DEVELOPERS.md](./DEVELOPERS.md) for dev environment setup and architecture details.

## License

Pulsy Atria is licensed under the Business Source License 1.1 (BSL 1.1). See [LICENSE](./LICENSE) for full terms. For a commercial license, reach out to sales@pulsy.app.
