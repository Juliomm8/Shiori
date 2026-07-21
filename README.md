# 🌸 Shiori

> **One library. Every story. Never lose your place.**

Shiori is a unified entertainment tracking platform built for people who love Anime, Manga, Light Novels, and Manhwa.

Instead of scattering your progress across streaming platforms, manga readers, spreadsheets, and aging tracking websites, Shiori gives you a single source of truth for your entire library—powered by modern backend engineering and designed to grow alongside the franchises you love.

---

## ✨ The Vision

Entertainment today is fragmented.

A single franchise can span multiple anime seasons, manga volumes, light novels, movies, spin-offs, and side stories. Meanwhile, your progress lives somewhere else:

- A streaming service remembers your last episode.
- A manga reader remembers your last chapter.
- A spreadsheet tracks your light novels.
- Another website tracks completed anime.
- None of them understand how everything connects.

Shiori exists to solve that fragmentation.

Rather than thinking in terms of disconnected media, Shiori thinks in terms of **franchises** and your personal journey through them.

Its mission is simple:

- **Never lose your progress.**
- **Always know what is available next.**
- **Understand how every story connects.**

Shiori is built around accuracy rather than assumptions. When release information is verified, it becomes actionable. When it isn't, Shiori refuses to guess.

The goal isn't simply another tracker.

It's a long-term platform for preserving your entertainment history.

---

# 🚀 Core Capabilities

The MVP focuses on solving the problems users experience every day.

## 📚 Unified Multi-Format Tracking

Track every type of content using the progress model it deserves.

- Anime (episodes & playback position)
- Manga
- Manhwa
- Light Novels

Each format supports its own native progress system while appearing together inside one unified library.

---

## 🧠 Release Intelligence

Know when new content is actually available.

Instead of generic "you're behind" notifications, Shiori compares your progress against verified release tracks and presents new chapters or episodes as opportunities—not obligations.

Examples include:

- Original Japanese releases
- Official English releases

When reliable data doesn't exist, Shiori simply doesn't invent it.

---

## 📥 Smart Staging Import

Bring years of history into Shiori safely.

Rather than immediately importing thousands of entries, every import follows three stages:

1. Upload
2. Preview
3. Confirm

Users can inspect matches, resolve conflicts, review progress conversions, and approve the final result before anything reaches their library.

Large imports are processed asynchronously, making the experience fast, reliable, and resilient.

---

## 🎯 Polymorphic Tracking

Not every medium works the same way.

Shiori treats different formats as first-class citizens instead of forcing everything into one generic progress model.

Examples include:

- Episode tracking
- Playback position
- Volume / Chapter / Page
- Decimal chapters
- Specials
- One-shots
- Extras
- Named chapters

Future formats can be introduced without redesigning the platform.

---

## 🗂 Franchise-Centric Catalog

Instead of isolated titles, Shiori understands relationships.

Browse:

- Adaptations
- Sequels
- Prequels
- Side stories
- Source material

All connected through a unified catalog so discovering "what comes next" becomes effortless.

---

## 🔒 Your Data Belongs To You

Shiori is designed around ownership.

Users can:

- Import existing libraries
- Export their data
- Keep lists private by default
- Share only what they choose

No vendor lock-in.

---

# 🏗 Architecture & Tech Stack

Shiori is a **backend-first** platform engineered for long-term scalability, reliability, and independent evolution.

Rather than optimizing for rapid prototypes, the project is built around clean service boundaries, explicit ownership, and production-grade architectural patterns.

## Backend

- **C#**
- **.NET 10**
- ASP.NET Core

## Architecture

- Microservices
  - Identity Service
  - Catalog Service
  - Tracking Service
- YARP API Gateway
- Event-Driven Communication

## Data Layer

- PostgreSQL
- MongoDB (Hybrid Document Model)
- RabbitMQ

## Engineering Principles

- Database-per-service
- Eventual consistency
- Transactional Outbox
- Idempotent Inbox
- Polyglot persistence
- OpenAPI-first APIs
- OAuth2 / OpenID Connect
- Docker-first local development

---

# 🐳 Getting Started

Shiori is fully containerized for local development.

The complete development environment—including databases, messaging infrastructure, and backend services—is orchestrated with **Docker Compose**, allowing contributors to get started with a single command once the repository is cloned.

Infrastructure includes:

- PostgreSQL
- MongoDB
- RabbitMQ
- API Gateway
- Identity Service
- Catalog Service
- Tracking Service

As additional milestones are completed, services are simply added to the existing Compose environment, keeping local development consistent with production architecture.

---

# 📍 Project Status

Shiori is currently in active development.

The initial milestone establishes the platform foundation:

- Identity Service
- API Gateway
- Containerized infrastructure
- Authentication
- CI pipeline
- Core service boundaries

Subsequent milestones introduce the Catalog, Tracking engine, Import pipeline, Release Intelligence, and the remaining MVP capabilities.

---

# 🤝 Contributing

Shiori is being built with a strong emphasis on:

- Clean Architecture
- Domain-Driven Design
- Explicit architectural decisions
- Production-grade engineering practices
- Comprehensive testing
- Long-term maintainability

Contributions are welcome as the project evolves.

---

# 📄 Documentation

Project documentation is organized into three foundational documents:

| Document | Purpose |
|----------|---------|
| **FEATURES.md** | Product vision and feature specifications |
| **ADR.md** | Architecture Decision Records |
| **ROADMAP.md** | Milestone-driven development roadmap |

---

# 🌸 Philosophy

Software should disappear behind the experience.

Shiori isn't trying to become another social network, recommendation engine, or content platform.

Its purpose is much simpler:

To become the most reliable place to remember every story you've experienced—and every story still waiting for you.
