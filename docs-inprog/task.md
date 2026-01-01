# UI/UX Redesign: Device-Child-User Management Flow

> **📍 YOU ARE HERE**: Task Tracker (Progress Checklist)
> 
> **How to Use This Document:**
> 1. Mark `[/]` when starting a task
> 2. Mark `[x]` when complete
> 3. Reference other docs for details on each phase
> 
> **Navigation:**
> - **Main Plan**: See [implementation_plan.md](implementation_plan.md) - Design & architecture
> - **Implementation Guide**: See [ui_implementation_guide.md](ui_implementation_guide.md) - Code for Phases 3-9
> - **What's Done**: See [walkthrough.md](walkthrough.md) - Completed work (Phases 1-2)
> - **Reference Docs**: [terminology_glossary.md](terminology_glossary.md) | [ux_writing_reference.md](ux_writing_reference.md)

## Task Breakdown

### Planning Phase ✅
- [x] Analyze existing data models and pages
- [x] Understand current user flows
- [x] **Phase 4 Verification**: Verify Unassigned Users page and Dashboard Alerts
- [x] **Phase 7 Verification**: Verify Child Profile Pages and Linking Logic
- [x] **Phase 8 Verification**: Verify Terminology Consistency ("Login" vs "Account")
- [x] **Phase 9 Verification**: Verify Empty States and Final Polish
- [x] **Critical Fix**: Fix MonitorUser.Settings Cardinality and Gateway Config Resolutionn updated design approach

---

### Implementation Phase

#### 1. Data Model Updates ✅
- [x] 1.1 Add ProfileId to MonitoredUserSettings entity
- [x] 1.2 Add navigation property to MonitoredProfile
- [x] 1.3 Update TgpDbContext configuration for new foreign key
- [x] 1.4 Create EF Core migration
- [x] 1.5 Test migration locally (ensure no errors)
- [x] 1.6 Commit & push migration
- [x] 1.7 Verify deployment and migration application

#### 2. Backend Services - Phase 1 (Core DTOs & Methods) ✅
- [x] 2.1 Create DeviceUserStateDto (unified state model)
- [x] 2.2 Rename ChildAccountLinkDto → ChildLoginLinkDto
- [x] 2.3 Backend foundations complete
- [x] 2.4 ChildrenService updated with Login terminology
- [x] 2.5 Commit & push backend changes

---

### Next Steps (See ui_implementation_guide.md)

A comprehensive implementation guide has been created for Phases 3-9.
See: **ui_implementation_guide.md** for complete, production-ready code and instructions.

**Remaining Phases**:

#### 3. Shared UI Components
- [x] 3.1 Create _UserStateBadgePartial.cshtml (pending/linked/ignored badges)
- [x] 3.2 Create _DetectedUserReviewPartial.cshtml (conversational flow component)
- [x] 3.3 Create CSS for user state badges
- [x] 3.4 Test components in isolation
- [x] 3.5 Commit & push components

#### 4. Update Existing Pages - Device Flow
- [x] 4.1 Dashboard/Index - Add "New logins found" alert
- [x] 4.2 Devices/Index - Add user counts to device cards
- [x] 4.3 Devices/Details - Major redesign:
  - [x] Add "New logins found" section with conversational flow
  - [x] Update "Logins I'm watching" (grouped by child + other)
  - [x] Add "Skipped logins" collapsible section
  - [x] Wire up inline child creation
- [x] 4.4 Devices/DetectedUsers - Rename & redesign:
  - [x] Change title to "New Logins to Review"
  - [x] Add conversational flow
  - [x] Add bulk actions
- [x] 4.5 Commit & push device flow updates

#### 5. Update UserSettings Page
- [x] 5.1 Add ProfileId as optional parameter
- [x] 5.2 Implement unassigned login view (basic monitoring only)
- [x] 5.3 Implement assigned login view (full settings)
- [x] 5.4 Add child selector dropdown (for multi-assigned logins)
- [x] 5.5 Update breadcrumb logic
- [x] 5.6 Add "Connect to child" banner for unassigned
- [x] 5.7 Update save logic to handle ProfileId
- [x] 5.8 Commit & push UserSettings updates

#### 6. Create New Pages
- [x] 6.1 Create Devices/UnassignedUsers page:
  - [x] Page model with list of unassigned logins
  - [x] UI showing all unassigned logins across devices
  - [x] Actions: Connect, Edit, Stop watching
  - [x] Bulk actions support
- [x] 6.2 Add navigation link to UnassignedUsers
- [x] 6.3 Commit & push new page

#### 7. Update Children Pages - Child Flow
- [x] 7.1 Children/Index - Add login counts to child cards
- [x] 7.2 Children/Details - Enhance for child-centric navigation:
  - [x] Show "Alice's Logins" grouped by device
  - [x] Add "Available to Connect" section with tabs
  - [x] Add quick actions to manage rules
- [x] 7.3 Commit & push children flow updates

#### 8. Terminology & Cleanup
- [x] 8.1 Global find/replace "account" → "login" in UI text
- [x] 8.2 Update all page titles and headers
- [x] 8.3 Review all button labels for consistency
- [x] 8.4 Remove any dead/commented code found
- [x] 8.5 Commit & push cleanup

#### 9. Final Integration & Polish
- [x] 9.1 Add SignalR real-time updates for new detected logins
- [x] 9.2 Add loading states and error handling
- [x] 9.3 Ensure all empty states have helpful messaging
- [x] 9.4 Test all navigation paths (device → child, child → device)
- [x] 9.5 Final commit & push

---

### Verification (Per Deployment)
After each commit:
- [ ] Monitor CI/CD build
- [ ] Verify deployment to Azure succeeds
- [ ] Test on live site: https://tgp-dashboard-prod.lemonsand-0fd9cbe0.northcentralus.azurecontainerapps.io/
- [ ] Check for runtime errors in logs

Final verification:
- [ ] Full user flow: Device detection → Assign to child → Configure rules
- [ ] Test unassigned login management
- [ ] Test both navigation paths (device-centric + child-centric)
- [ ] Verify tenant data isolation
- [ ] Test edge cases (delete child with logins, etc.)

---

### Documentation
- [x] Create walkthrough.md showing new UI flows
- [ ] Update any user-facing documentation

