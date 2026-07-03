# Auth Service — Security Threat Model

## Trust Boundaries
- **Browser** → **API**: HTTPS, JWT in Authorization header
- **API** → **Database**: Internal network (Docker), PostgreSQL

## Assets
- User credentials (email + password hash)
- JWT tokens
- User profile data

## Threats & Mitigations

| Threat | Impact | Mitigation |
|--------|--------|------------|
| Brute force login | Account takeover | BCrypt (slow hash), rate limiting (future) |
| JWT theft | Session hijacking | HTTPS, short expiry (24h), no PII in token |
| SQL injection | Data leak | EF Core parameterized queries |
| Privilege escalation | Unauthorized admin access | Role-based authorization via `[Authorize(Roles)]` |
| Timing attack | Email enumeration | BCrypt.Verify regardless of email existence |
| Token replay | Session replay | JTI claim, short expiry |
