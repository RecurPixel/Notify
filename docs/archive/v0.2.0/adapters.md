---
layout: default
title: "Adapter Reference (v0.2.0 Archive)"
nav_exclude: true
search_exclude: true
---

> **This is an archived snapshot of the v0.2.0 adapter reference.**  
> For the current v0.3.0 docs (includes MSG91 SMS and WhatsApp) see [Adapter Reference](../../adapters).

# Adapter Reference (v0.2.0)

All available channel adapters in v0.2.0, their configuration fields, native bulk support, and channel-specific `Metadata` keys.

Install all packages at version `0.2.0`:

```bash
dotnet add package RecurPixel.Notify.Email.SendGrid --version 0.2.0
# (replace package name as needed)
```

For the full per-provider config reference at v0.2.0, see the snapshot of the getting-started guide and the v0.2.0 NuGet packages.

The v0.2.0 adapter set is identical to v0.3.0 except v0.3.0 adds:
- `RecurPixel.Notify.Sms.Msg91` — MSG91 SMS
- `RecurPixel.Notify.WhatsApp.Msg91` — MSG91 WhatsApp Business

All other adapters are unchanged between v0.2.0 and v0.3.0.
