# TGP Terminology Glossary

> **📍 YOU ARE HERE**: Reference - Terminology Standards
> 
> **Purpose**: Use this as a reference while implementing to ensure terminology consistency
> 
> **Navigation:**
> - **Main Plan**: See [implementation_plan.md](implementation_plan.md)
> - **Implementation Guide**: See [ui_implementation_guide.md](ui_implementation_guide.md) - Phases 3-9
> - **Other Reference**: [ux_writing_reference.md](ux_writing_reference.md) - UX copy guidelines

## Core Concepts

### The Hierarchy

```
Tenant/Family
  └─ Parent Account (You)
      ├─ Child Profile: "Alice"
      │   ├─ Device Login: alice@laptop (Windows PC)
      │   ├─ Device Login: alice (iPad)
      │   └─ Device Login: alice_school (School Chromebook)
      ├─ Child Profile: "Bob"
      │   ├─ Device Login: bob@laptop (Windows PC)
      │   └─ Device Login: bobby (iPhone)
      └─ Family Member Account: "Co-parent Sarah"
```

**Key Insight**: A **Child Profile** is a container. A **Device Login** is a username on a device. One child can have many device logins across different devices.

---

## Official Terminology

### 1. Parent Account / You
**Technical**: `ApplicationUser` (with parent role)
**UI Term**: "You", "Your account", "Parent account"

**Usage:**
- ✅ "Your dashboard"
- ✅ "Invite a co-parent"
- ❌ "User dashboard" (too vague)
- ❌ "Admin account" (too technical)

---

### 2. Child Profile
**Technical**: `MonitoredProfile`
**UI Term**: "Child" or child's name ("Alice", "Bob")

**What it is**: An organizational profile representing one of your children. This is NOT an account; it's a way to group all of a child's device accounts together.

**Usage:**
- ✅ "Alice" (use the child's name directly)
- ✅ "Your children"
- ✅ "Create a child profile"
- ✅ "Add a child"
- ❌ "Profile" (too vague without context)
- ❌ "Child account" (confusing - implies a login account)
- ❌ "Monitored profile" (too technical)

**Visual Representation**: Always show with avatar/color to distinguish from device accounts

---

### 3. Device
**Technical**: `Device`
**UI Term**: "Device" or device name ("Living Room PC", "Alice's iPad")

**What it is**: A physical computer, tablet, or phone running TGP client software.

**Usage:**
- ✅ "Living Room PC"
- ✅ "Devices"
- ✅ "Alice's iPad" (when ownership is clear)
- ❌ "Machine" (too technical)
- ❌ "Endpoint" (too technical)

**Naming Convention**: Encourage parents to give devices friendly, location-based names

---

### 4. Device Login (Device Username)
**Technical**: `MonitoredUser` (represents a Windows/Mac/device user account)
**UI Term**: "Login" or "[username] on [Device]"

**What it is**: A username that someone uses to log in to a specific device. This is what the child actually types to access Windows, macOS, etc.

**CRITICAL DISTINCTION**: 
- A **child profile** is organizational (Alice the person)
- A **device login** is technical (alice@laptop on Windows)
- One child can have many device logins
- "Account" is reserved for TGP dashboard users (parent, co-parent)

**Usage:**
- ✅ "alice@laptop" (show the actual username)
- ✅ "Login on Living Room PC"
- ✅ "Alice's logins" (when listing all device logins for Alice)
- ✅ "Windows login", "Mac login" (when context helps)
- ✅ "User login" (when being more explicit)
- ❌ "Account" (confusing with TGP accounts)
- ❌ "User" (too vague)
- ❌ "Device user" (too technical)
- ❌ "Monitored user" (too technical)

**Visual Representation**: Always show WITH the device name/icon to make it clear this is a login ON a device

**Example Phrasing:**
```
Alice's Logins:
  🖥️ alice@laptop (Living Room PC)
  📱 alice (iPad)
  💻 alice_school (School Chromebook)
```

---

### 5. Parent Account / Family Member Account
**Technical**: `ApplicationUser` (with roles)
**UI Term**: "Your account" (parent), "Co-parent account", "Family member"

**What it is**: An adult's login to the TGP dashboard (not a device login).

**Usage:**
- ✅ "Your account" (for the logged-in parent)
- ✅ "Invite a co-parent"
- ✅ "Family member account"
- ✅ "Sarah (Co-parent)"
- ❌ "User" (too vague)
- ❌ "Secondary user" (too technical)

---

## Relationship Terms

### Connecting a Device Login to a Child Profile
**Action**: "Connect"
**UI Term**: "Connect [username] to [Child]"

**Usage:**
- ✅ "Connect this login to Alice"
- ✅ "Connect alice@laptop to Alice"
- ✅ "Which child uses this login?"
- ❌ "Link profile to user"
- ❌ "Associate monitored user with profile"

---

### Disconnecting
**Action**: "Disconnect" or "Unassign"
**UI Term**: "Disconnect from [Child]"

**Usage:**
- ✅ "Disconnect from Alice"
- ✅ "This account is no longer connected to any child"
- ❌ "Unlink"
- ❌ "Disassociate"

---

## Common UI Scenarios

### Scenario 1: Device with Multiple Logins
```
Living Room PC
├─ Alice's Logins (2)
│   ├─ alice@laptop
│   └─ alice_homework
├─ Bob's Logins (1)
│   └─ bob@laptop
└─ Other Logins (1)
    └─ guest_user (not connected)
```

**Phrasing**: 
- "Alice has 2 logins on this device"
- "Bob's login: bob@laptop"
- "1 other login not connected to a child"

---

### Scenario 2: Child with Multiple Devices
```
Alice
├─ Living Room PC
│   └─ alice@laptop
├─ iPad
│   └─ alice
└─ School Chromebook
    └─ alice_school
```

**Phrasing**:
- "Alice uses 3 devices"
- "alice@laptop on Living Room PC"
- "Manage rules for alice@laptop"

---

### Scenario 3: Detected New Login
```
We found a new login on Living Room PC:
  Username: "john_smith"
  
Question: Is 'john_smith' one of your children?
  [Yes] [No, skip this login]

If Yes:
  Would you like to:
  ( ) Connect to Alice
  ( ) Connect to Bob
  ( ) Create new child profile
```

**This clearly shows**: We're asking about a device login ("john_smith"), and connecting it to a child profile (Alice/Bob).

---

## Error Prevention

### DON'T Mix Contexts
❌ BAD: "Add user to profile"
  → Unclear what "user" means

✅ GOOD: "Connect login to Alice"
  → Clear: login (device username) → Alice (child profile)

### ALWAYS Show Device with Login
❌ BAD: "alice" (could be on any device)
✅ GOOD: "alice on iPad"
✅ GOOD: "alice@laptop (Living Room PC)"

### Use Possessive for Clarity
✅ "Alice's logins" (multiple device logins)
✅ "Alice's iPad" (the device)
✅ "Rules for alice@laptop" (specific device login)

---

## Implementation Checklist

When writing ANY UI copy, ask:
- [ ] Am I talking about a child (person) or a device login (username)?
- [ ] If device login, did I include the device name/context?
- [ ] Am I using "login" for device and "account" for TGP dashboard?
- [ ] Would a non-technical parent understand this?
- [ ] Does the visual design reinforce the distinction (icons, grouping)?
