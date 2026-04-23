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

> **Beta timeline**: Atria Cloud (the managed reflection of this repo) enters public beta in **Q2 2026**. [Apply for Early Access](https://pulsy.app/contact-atria).

## Overview

Use Atria to monitor high-impact transactions, DEX liquidity shifts, bridge flows, stablecoin movements, governance votes, and more. You define a feed, and Atria handles ingestion, real-time processing, and delivery.

## Quick Start
Run Atria locally using Docker:
```bash
curl -fsSL https://raw.githubusercontent.com/Pulsy-Global/atria/main/deploy/docker/install.sh | bash
cd ./atria-oss/prod
docker compose up -d
```
> You can configure the environment via the generated `.env` file.

## Documentation
Getting started guides and core concepts for Atria are available on our
**[documentation site](https://docs.pulsy.app/atria/getting-started/overview)**.

## Development

See [DEVELOPERS.md](./DEVELOPERS.md) for dev environment setup and architecture details.

## License

Pulsy Atria is licensed under the Business Source License 1.1 (BSL 1.1). See [LICENSE](./LICENSE) for full terms. For a commercial license, reach out to sales@pulsy.app.
