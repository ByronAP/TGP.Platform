# UX Writing Reference Guide

> **📍 YOU ARE HERE**: Reference - UX Copy Guidelines
> 
> **Purpose**: Use this as a reference while writing UI copy to ensure parent-friendly language
> 
> **Navigation:**
> - **Main Plan**: See [implementation_plan.md](implementation_plan.md)
> - **Implementation Guide**: See [ui_implementation_guide.md](ui_implementation_guide.md) - Phases 3-9
> - **Other Reference**: [terminology_glossary.md](terminology_glossary.md) - Terminology standards

## Do's and Don'ts for Parent-Friendly Copy

### ✅ DO Use Simple, Parent-Oriented Language

| Instead of... | Use... |
|--------------|--------|
| "Monitored user detected on device" | "We found a new account on Living Room PC" |
| "Link profile to monitored user" | "Connect to Alice" |
| "Configure monitoring policy" | "Set rules for Alice" |
| "Unlink monitored profile" | "Disconnect from Alice" |
| "Ignored users list" | "Accounts I'm not watching" |
| "Device user settings" | "Rules for this account" |

### ❌ DON'T Use Technical Jargon

- MonitoredUser, DetectedUser, ProfileLink
- "Entities", "Configurations", "Policies"
- Database terms in error messages
- Developer terminology in UI labels

### 🎯 Button Labels That Work

```
Good Buttons:
- "Set up for Alice"
- "Manage rules"
- "Skip this account"
- "Connect to..."
- "Copy Alice's rules"

Bad Buttons:
- "Assign to profile"
- "Configure settings"
- "Ignore user"
- "Link entity"
- "Duplicate configuration"
```

### 💬 Microcopy Examples

**When showing detected user:**
```
✅ "Is 'JohnDoe' one of your children?"
❌ "Detected user 'JohnDoe' requires review."
```

**When no children exist:**
```
✅ "Add your children's profiles to organize monitoring"
❌ "No monitored profiles configured for this tenant"
```

**When unlinked user:**
```
✅ "This account isn't connected to any child yet"
❌ "MonitoredUser has no ProfileLink associations"
```

### 🏗️ Progressive Disclosure Examples

**Basic View (Default):**
- Show: Screen Time, Websites, Apps
- Hide: Keylogging, Clipboard monitoring, Technical settings

**Advanced View (Toggle):**
- Reveal: "Show advanced options" expands section
- Label clearly: "Advanced monitoring (for older kids)"
- Explain: Brief tooltip on why you might use it

## Implementation Checklist

- [ ] Review all button labels
- [ ] Update all page titles and headers
- [ ] Write parent-friendly empty states
- [ ] Add helpful microcopy throughout
- [ ] Test copy with non-technical user
- [ ] Ensure error messages are actionable
- [ ] Use consistent terminology across all pages
