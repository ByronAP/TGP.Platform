I will upgrade `TGP.UserDashboard` into a fully functional microservice that handles both its UI and backend logic, as requested.

## Phase 1: Microservice Architecture (Agents: `devops-architect` / `backend-architect`)
I will task the **Backend Architect** (supported by DevOps) to:
1.  **Dependencies**: Add `TGP.Data` reference to `TGP.UserDashboard`.
2.  **Configuration**: Register `TgpDbContext` (PostgreSQL) and **RabbitMQ** services in `Program.cs`.
3.  **Local Backend Services**: Implement domain logic directly within `TGP.UserDashboard`:
    *   `DeviceManagementService`: Handles direct database updates for configurations and publishes `DeviceCommand` events to RabbitMQ.
    *   `AlertsService`: Queries `TgpDbContext` for recent alerts.

## Phase 2: Frontend & Core Features (Agent: `frontend-architect`)
I will task the **Frontend Architect** to:
1.  **Refactor Pages**: Update `Dashboard/Index`, `Devices/Details`, and `Devices/Commands` to use the new local services instead of external API calls for device management.
2.  **Core UI Functionality**:
    *   **User Menu**: Add a proper dropdown in `_Layout.cshtml` with Profile and **Logout** actions.
    *   **Theme Switcher**: Implement a Light/Dark/Auto mode toggle with persistence (cookie/local storage) and CSS variables support.
3.  **Authentication**: Ensure the cookie-based auth flow is robust and supports the new Logout functionality.

## Phase 3: UI/UX Polish (Agent: `ui-designer`)
I will task the **UI Designer** to:
1.  **Visual Overhaul**: Update `site.css` to support the new theming system and ensure a consistent, high-quality design.
2.  **UX Improvements**: Implement proper empty states and loading feedback for all dynamic sections.
