# RECONQUEST

<p align="center">
  <img src="Assets/Logo/logo.png" alt="Reconquest Logo" width="200"/>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Motor-Unity%206-black?style=flat-square&logo=unity"/>
  <img src="https://img.shields.io/badge/Lenguaje-C%23-239120?style=flat-square&logo=csharp"/>
  <img src="https://img.shields.io/badge/Plataforma-Windows%2064bit-0078D6?style=flat-square&logo=windows"/>
  <img src="https://img.shields.io/badge/Estado-v1.0.0-576533?style=flat-square"/>
</p>

---

## Descripción

**Reconquest** es un videojuego de estrategia en tiempo real (RTS) desarrollado con **Unity** y **C#** como Trabajo de Fin de Grado.

El juego se ambienta en una ucronía histórica situada en Portugal, donde el fracaso de la **Revolución de los Claveles de 1974** deriva en una guerra civil ficticia. El jugador asume el rol de comandante de una de las facciones en conflicto, gestionando tropas, recursos y territorios para lograr el control total del mapa.

---

## Características principales

- **Conquista de fábricas** — Los recursos no se recolectan pasivamente. Captura y mantén nodos industriales distribuidos por el mapa para financiar tu ejército y construir defensas.
- **Gestión de tropas** — Spawnea y organiza unidades de combate. Equilibra capacidades ofensivas y defensivas para avanzar sobre el territorio enemigo.
- **Control de sectores** — Domina cada sector estratégico del mapa. La victoria llega con la destrucción de la base enemiga principal.
- **IA y pathfinding** — Ciclo de simulación en tiempo real que gestiona la inteligencia artificial de las unidades y su navegación por el entorno de combate.

---

## Arquitectura del software

El proyecto se sustenta en tres subsistemas principales:

| Subsistema | Descripción |
|---|---|
| **Gestión de entidades** | Control del ciclo de vida, spawning y organización de unidades de combate |
| **Control de recursos** | Flujo económico basado en la conquista y mantenimiento de fábricas |
| **Captura de sectores** | Sistema de nodos que determina el avance estratégico y la condición de victoria |

---

## Requisitos del sistema

| Componente | Mínimo |
|---|---|
| Sistema operativo | Windows 10 / 11 (64-bit) |
| RAM | 4 GB |
| Tarjeta gráfica | DirectX 11 compatible |
| Almacenamiento | ~500 MB libres |

---

## Instalación

1. Descarga el instalador desde la [web oficial](https://reconquest-web.onrender.com)
2. Ejecuta `Reconquest_Setup_v1.0.0.exe`
3. Si aparece el aviso de Windows SmartScreen, haz clic en **"Más información" → "Ejecutar de todas formas"**
4. Sigue el asistente de instalación

---

## Desarrollo

### Tecnologías utilizadas

- **Motor:** Unity 6
- **Lenguaje:** C#
- **Control de versiones:** Git / GitHub
- **Web:** Python (Flask) + HTML/CSS/JS + SQLite

### Estructura del repositorio

```
Reconquest/
├── Assets/
│   ├── Scripts/        ← Código C# del juego
│   ├── Prefabs/        ← Prefabs de unidades, edificios, etc.
│   ├── Scenes/         ← Escenas de Unity
│   ├── UI/             ← Elementos de interfaz
│   └── Logo/           ← Logotipo del juego
├── Packages/
├── ProjectSettings/
└── README.md
```

---

## Contexto histórico

La **Revolución de los Claveles** fue un golpe de estado militar que tuvo lugar en Portugal el 25 de abril de 1974, poniendo fin a casi 50 años de dictadura. En la realidad histórica, la revolución triunfó pacíficamente.

Reconquest parte de una **ucronía**: ¿qué habría pasado si la revolución hubiera fracasado? El resultado es una guerra civil ficticia que sirve de escenario para el juego.

---

## Créditos

Desarrollado como **Trabajo de Fin de Grado**.

- **Desarrollador:** *Tu nombre aquí*
- **Tutor:** *Nombre del tutor*
- **Universidad:** *Nombre de la universidad*
- **Año:** 2025

---

## Licencia

Este proyecto ha sido desarrollado con fines académicos. Todos los derechos reservados.