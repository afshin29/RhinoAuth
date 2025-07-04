<h1>
  <img src="./logo.png" alt="Logo" width="60" style="vertical-align: middle; margin-right: 8px;">
  RhinoAuth
</h1>
<hr/>

RhinoAuth is a simple cloud native OAuth 2.1 and OpenID Connect server (and a work in progress). Please note that this is not a library, but a final product. Since different projects need different identity management features, this project cannot satisfy everyone. But, at least I hope it can be used as a starting template.

Also please note that this project needs external services such as Captcha, email sender, SMS sender, etc. which are not implemented with any real provider and only a development implementation is available.

## State of the specs
🟢 Implemented  
🔴 Not Implemented  
🔘 Not Needed

| Spec| Title | State| Note |
| :------------ |:---------------|:-----:| --------- |
| [RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749) | OAuth 2.0 | 🔘 | Obsolete in favor of OAuth 2.1 |
| [RFC 6750](https://datatracker.ietf.org/doc/html/rfc6750) | Bearer Token Usage | 🟢 ||
| [RFC 7009](https://datatracker.ietf.org/doc/html/rfc7009) | Token Revocation | 🔴 ||
| [RFC 7516](https://datatracker.ietf.org/doc/html/rfc7516) | Json Web Encryption | 🔴 ||
| [RFC 7519](https://datatracker.ietf.org/doc/html/rfc7519) | Json Web Token | 🟢 ||
| [RFC 7522](https://datatracker.ietf.org/doc/html/rfc7522) | SAML for Client Authentication | 🔴 ||
| [RFC 7523](https://datatracker.ietf.org/doc/html/rfc7523) | JWT for Client Authentication | 🔴 ||
| [RFC 7591](https://datatracker.ietf.org/doc/html/rfc7591) | Dynamic Client Registration | 🔴 ||
| [RFC 7636](https://datatracker.ietf.org/doc/html/rfc7636) | PKCE | 🟢 | Mandatory in OAuth 2.1 |
| [RFC 7662](https://datatracker.ietf.org/doc/html/rfc7662) | Token Introspection | 🔘 | Not needed because of using self-signed tokens |
| [RFC 7800](https://datatracker.ietf.org/doc/html/rfc7800) | Proof-of-Possession Key Semantics for JWT | 🔴 ||
| [RFC 8414](https://datatracker.ietf.org/doc/html/rfc8414) | Authorization Server Metadata | 🟢 ||
| [RFC 8628](https://datatracker.ietf.org/doc/html/rfc8628) | Device Authorization Grant | 🔴 ||
| [RFC 8693](https://datatracker.ietf.org/doc/html/rfc8693) | Token Exchange | 🔴 ||
| [RFC 8705](https://datatracker.ietf.org/doc/html/rfc8705) | Mutual-TLS Client Authentication | 🔴 ||
| [RFC 8707](https://datatracker.ietf.org/doc/html/rfc8707) | Resource Indicators | 🟢 ||
| [RFC 9068](https://datatracker.ietf.org/doc/html/rfc9068) | JWT Profile for Access Tokens | 🔴 ||
| [RFC 9101](https://datatracker.ietf.org/doc/html/rfc9101) | JWT-Secured Authorization Request (JAR) | 🔴 ||
| [RFC 9126](https://datatracker.ietf.org/doc/html/rfc9126) | Pushed Authorization Requests (PAR) | 🔴 ||
| [RFC 9207](https://datatracker.ietf.org/doc/html/rfc9207) | Authorization Server Issuer Identification | 🟢 ||
| [RFC 9396](https://datatracker.ietf.org/doc/html/rfc9396) | Rich Authorization Requests | 🔴 ||
| [RFC 9449](https://datatracker.ietf.org/doc/html/rfc9449) | Demonstrating Proof of Possession (DPoP) | 🔴 ||
| [RFC 9470](https://datatracker.ietf.org/doc/html/rfc9470) | Step Up Authentication Challenge Protocol | 🔴 ||
| [draft](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1-13)      | OAuth 2.1 | 🟢 ||
|  | [OpenID Connect Core](https://openid.net/specs/openid-connect-core-1_0.html) | 🟢 | Partially implemented |
|  | [OpenID Connect Discovery](https://openid.net/specs/openid-connect-discovery-1_0.html) | 🟢 ||
|  | [OpenID Connect Dynamic Client Registration](https://openid.net/specs/openid-connect-registration-1_0.html) | 🔴 ||
|  | [OpenID Connect Session Management](https://openid.net/specs/openid-connect-session-1_0.html) | 🔴 ||
|  | [OpenID Connect Front-Channel Logout](https://openid.net/specs/openid-connect-frontchannel-1_0.html) | 🔘 | Back-Channel is better |
|  | [OpenID Connect Back-Channel Logout](https://openid.net/specs/openid-connect-backchannel-1_0.html) | 🟢 ||


## Limitations and known issues

Only the `code` OpenID Connect flow is supported to follow OAuth 2.1. It is possible that OpenID Connect will remove the `implicit` and `hybrid` flows in a future updated spec.

Currently only ECDSA JSON Web Key (JWK) is supported, because it is superior to RSA. However, according to specs, supporting RSA is mandatory.

Currently the following OpenID Connect Core parameters are not recognized:

- display
- prompt
- max_age
- ui_locales
- id_token_hint
- login_hint
- acr_values

Out of all of these parameters, the `prompt` is really useful, because clients can check users' auth state and they can make users reenter their password for sensitive operations. It's unfortunate that the OIDC spec merged this concept with the authorization flow, because it makes the flow even more complicated than it already is. In my opinion it should have been a separate endpoint and flow.

According to OAuth, generating a refresh token is optional and up to the server. According to OpenID Connect, generating a refresh token depends on the `offline_access` scope and if persent, a consent page must be shown.
Currently a refresh token will always be generated and `offline_acess` will not be processed, which is acceptable for 1st-party clients.


## Missing features (apart from incomplete ones)

- Admin endpoints and Admin UI
- Public client registration endpoint and UI
- User management APIs for native app clients
- Multi-factor authentication (ideally TOTP)
- Publishing events to a message broker
- Documentation