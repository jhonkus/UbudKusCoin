# Compliance & Legal Positioning

**Purpose**
- This document centralizes the legal and regulatory positioning of **UbudKusChain** for developers, integrators, and end‑users.

**Key Positioning Statement**
- UbudKusChain is a **blockchain infrastructure** designed for **business and community applications** (loyalty, vouchers, identity, supply‑chain, ERP audit trails, etc.).
- The native **UKSC token** is **not** a legal tender, nor is it intended for retail‑level payment of goods or services in Indonesia.
- All applications built on top of the chain must **comply with applicable local laws and regulations**, including but not limited to:
  - **Bank Indonesia** regulations concerning payment systems and electronic money.
  - **Otoritas Jasa Keuangan (OJK)** guidelines for financial technology and digital asset services.
  - **Consumer Protection Law** and **Data Privacy (PDP Law)** for handling personal data.

**Regulatory Guidance for Integrators**
1. **Do not market UKSC as a currency or payment method.**
   - Avoid language such as “pay with UKSC”, “UKSC as a replacement for Rupiah”, or “cryptocurrency payments”.
2. **If you issue business‑level assets (loyalty points, vouchers, certificates, etc.),** ensure they are clearly defined as *utility* or *membership* tokens with:
   - Explicit issuance limits.
   - Defined expiry dates and redemption rules.
   - No automatic conversion path to fiat currency.
3. **Perform KYC/AML checks** where required by law (e.g., for services that approach the definition of a payment service provider).
4. **Maintain audit trails** using the blockchain’s immutable state root for compliance reporting.
5. **Secure user data** according to Indonesia’s Personal Data Protection (PDP) requirements – encryption at rest, minimal data collection, and clear privacy notices.

**Documentation Requirements**
- All public‑facing documentation (README, website, SDK docs) must include the **Positioning Disclaimer** (see README) and a reference to this compliance guide.
- PRs that modify the README or any marketing material must pass the **Contributor Checklist** (see CONTRIBUTING.md).

**Legal Disclaimer**
- The UbudKusCoin project does not provide legal advice. Implementers are responsible for obtaining appropriate legal counsel to ensure compliance with local regulations.
- The project team disclaims any liability for misuse of the platform contrary to the intended business‑focused use‑case.

**Contact**
- For questions about compliance, please open an issue in the repository or contact the maintainers at `legal@ubudkuscoin.org`.
