# NuGet Package Signing Investigation

This document provides a comprehensive investigation of NuGet package signing for the LicenseManager_12noon.Client package, including security benefits, advantages/disadvantages, and implementation considerations.

## Overview

NuGet package signing is a security feature that allows package authors to digitally sign their packages using a code signing certificate. This provides tamper protection and author verification for packages published to NuGet.org and other NuGet feeds.

Reference: [Signed packages reference - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference)

## How is it More Secure?

### 1. **Author Identity Verification**
- Signed packages include a cryptographic signature that proves the package came from the stated author
- The signature is tied to a certificate issued by a trusted Certificate Authority (CA)
- Users can verify that the package hasn't been tampered with since it was signed

### 2. **Tamper Protection**
- Any modification to the package after signing will invalidate the signature
- NuGet clients can detect if a package has been modified in transit or in storage
- Prevents man-in-the-middle attacks where malicious code could be injected

### 3. **Package Integrity**
- The signature ensures the complete integrity of the package contents
- Hash verification confirms that the downloaded package matches what was originally published
- Protects against corruption during transmission or storage

### 4. **Trust Chain**
- Certificates are issued by trusted Certificate Authorities (CAs)
- The trust chain can be verified from the package certificate back to a root CA
- Similar security model to HTTPS/TLS certificates

### 5. **Repository Signature (NuGet.org)**
- NuGet.org automatically adds a repository signature to all uploaded packages
- Provides an additional layer of verification that the package came from NuGet.org
- Even unsigned packages get repository signatures

## Advantages

### Security Benefits
1. **Enhanced Trust**: Users can verify the authenticity of your package
2. **Compliance**: Some organizations require signed packages for security compliance
3. **Brand Protection**: Prevents others from publishing malicious packages claiming to be from you
4. **Supply Chain Security**: Part of a comprehensive software supply chain security strategy

### Operational Benefits
1. **Professional Image**: Demonstrates commitment to security best practices
2. **Enterprise Adoption**: Many enterprise environments prefer or require signed packages
3. **NuGet.org Badge**: Signed packages may receive visual indicators on NuGet.org (verification badges)
4. **Audit Trail**: Certificate information provides an audit trail of who published the package

### Technical Benefits
1. **Automated Verification**: NuGet clients automatically verify signatures during installation
2. **Policy Enforcement**: Organizations can configure NuGet clients to require signed packages
3. **Certificate Revocation**: If a certificate is compromised, it can be revoked to invalidate all packages signed with it

## Disadvantages

### Cost
1. **Certificate Purchase**: Code signing certificates cost $200-$500+ per year
	- Extended Validation (EV) certificates: $300-$500/year
	- Organization Validation (OV) certificates: $200-$400/year
	- Popular vendors: DigiCert, Sectigo, GlobalSign
2. **Renewal**: Annual or multi-year renewal costs
3. **Company Verification**: EV certificates require business verification (Dun & Bradstreet number, legal documents)

### Complexity
1. **Certificate Management**: 
	- Secure storage of certificate and private key
	- Certificate renewal process
	- Handling certificate expiration
2. **Build Process Changes**: Additional steps in CI/CD pipeline
3. **Secret Management**: Securely storing certificate in GitHub Secrets or other secret management systems

### Operational Overhead
1. **Certificate Installation**: Setting up certificate on build server
2. **Password Management**: Certificate password must be securely stored and accessed
3. **Timestamp Server**: Dependency on timestamp servers for long-term signature validity
4. **Troubleshooting**: Additional failure points in the build/publish process

### Technical Limitations
1. **Signature Expiration**: Signatures can expire if not timestamped properly
2. **Certificate Revocation**: If certificate is revoked, all signed packages become invalid
3. **No Free Options**: Unlike SSL/TLS certificates (Let's Encrypt), there are no free code signing certificates
4. **Platform Specific**: Some certificate types require Windows for signing

## Impact on CI/CD (build.yml)

### Current State
The current `build.yml` workflow:
- Builds the NuGet package on Windows runner
- Packs the package using `dotnet pack`
- Publishes to NuGet.org using `dotnet nuget push`
- No signing currently implemented

### Required Changes for Package Signing

#### 1. **Certificate Storage**
Add the certificate to GitHub Secrets:
```yaml
# In GitHub repository settings > Secrets and variables > Actions
# Add these secrets:
# - SIGNING_CERT_BASE64: Base64-encoded .pfx certificate file
# - SIGNING_CERT_PASSWORD: Certificate password
```

#### 2. **Modified Build Steps**
Add signing step after packing:

```yaml
# In build.yml, add after the pack step (line ~179):

- name: 🔐 Decode and install signing certificate
  if: ${{ startsWith(github.ref, 'refs/tags/v') }}
  shell: pwsh
  run: |
    $certBytes = [Convert]::FromBase64String($env:SIGNING_CERT_BASE64)
    $certPath = Join-Path $env:TEMP "signing-cert.pfx"
    [IO.File]::WriteAllBytes($certPath, $certBytes)
    echo "CERT_PATH=$certPath" >> $env:GITHUB_ENV
  env:
    SIGNING_CERT_BASE64: ${{ secrets.SIGNING_CERT_BASE64 }}

- name: ✍️ Sign NuGet package
  if: ${{ startsWith(github.ref, 'refs/tags/v') }}
  run: |
    dotnet nuget sign ./release/*.nupkg `
      --certificate-path $env:CERT_PATH `
      --certificate-password $env:SIGNING_CERT_PASSWORD `
      --timestamper http://timestamp.digicert.com `
      --overwrite
  env:
    SIGNING_CERT_PASSWORD: ${{ secrets.SIGNING_CERT_PASSWORD }}

- name: 🧹 Clean up certificate
  if: ${{ always() && startsWith(github.ref, 'refs/tags/v') }}
  shell: pwsh
  run: |
    if (Test-Path $env:CERT_PATH) {
      Remove-Item $env:CERT_PATH -Force
    }
```

#### 3. **Certificate Upload to NuGet.org** (Optional)
- Upload the certificate (.cer file, public key only) to NuGet.org
- This associates your certificate with your NuGet.org account
- Provides additional author verification on the package page
- Steps:
	1. Export public certificate (.cer) from .pfx file
	2. Go to NuGet.org account settings
	3. Upload certificate under "Certificates"
	4. Certificates are shown on package pages for author verification

#### 4. **Verify Signed Package**
Add verification step:

```yaml
- name: 🔍 Verify package signature
  if: ${{ startsWith(github.ref, 'refs/tags/v') }}
  run: |
    dotnet nuget verify ./release/*.nupkg --all
```

### Timestamp Servers
- **DigiCert**: http://timestamp.digicert.com
- **Sectigo**: http://timestamp.sectigo.com
- **GlobalSign**: http://timestamp.globalsign.com

Timestamping ensures signatures remain valid even after the certificate expires.

## Implementation Steps

### Phase 1: Certificate Acquisition
1. **Choose Certificate Authority**: DigiCert, Sectigo, GlobalSign, etc.
2. **Select Certificate Type**:
	- **EV Code Signing**: More expensive, higher trust, requires hardware token
	- **OV Code Signing**: Less expensive, standard trust, software-based
3. **Purchase Certificate**: $200-$500/year depending on type and vendor
4. **Complete Verification**: Provide business verification documents
5. **Receive Certificate**: Download .pfx file with private key

### Phase 2: GitHub Configuration
1. **Export Certificate**:
	```powershell
	# Convert .pfx to base64 for GitHub Secret
	$certBytes = [System.IO.File]::ReadAllBytes("path\to\cert.pfx")
	$certBase64 = [Convert]::ToBase64String($certBytes)
	$certBase64 | Set-Clipboard
	```

2. **Add GitHub Secrets**:
	- Navigate to repository Settings > Secrets and variables > Actions
	- Add `SIGNING_CERT_BASE64` with the base64 string
	- Add `SIGNING_CERT_PASSWORD` with the certificate password

3. **Test Locally** (optional):
	```bash
	# Sign package locally
	dotnet nuget sign package.nupkg \
		--certificate-path cert.pfx \
		--certificate-password "password" \
		--timestamper http://timestamp.digicert.com
	
	# Verify signature
	dotnet nuget verify package.nupkg --all
	```

### Phase 3: Update CI/CD Pipeline
1. Update `build.yml` with certificate decoding, signing, and verification steps (see above)
2. Test the workflow on a non-production branch first
3. Verify the package is signed correctly before publishing

### Phase 4: NuGet.org Configuration (Optional)
1. Export public certificate (.cer):
	```powershell
	# Export public key from .pfx
	$cert = Get-PfxCertificate -FilePath "cert.pfx"
	Export-Certificate -Cert $cert -FilePath "cert.cer"
	```

2. Upload to NuGet.org:
	- Go to https://www.nuget.org/account/Certificates
	- Click "Register new"
	- Upload the .cer file
	- Packages signed with this certificate will show verification badge

### Phase 5: Documentation
1. Update README.md to mention package signing
2. Document certificate renewal process
3. Create runbook for certificate management

## Recommendations

### For This Project

Given the nature of the LicenseManager_12noon.Client package:

**Recommendation: Consider signing, but prioritize based on user base**

#### Reasons to Sign:
- **Security-focused product**: This is a licensing/security library, so signing demonstrates security commitment
- **Enterprise users**: Many enterprises using licensing software may require signed packages
- **Professional image**: Aligns with the professional quality of the product
- **Supply chain security**: Part of modern security best practices

#### Reasons to Delay:
- **Cost**: $200-$500/year is a recurring expense
- **Complexity**: Additional maintenance overhead
- **Current security**: NuGet.org already applies repository signatures to all packages
- **User demand**: No explicit user requests for signed packages yet

### Suggested Approach

**Phase 1 (Immediate):**
1. Research certificate costs from different vendors
2. Monitor user feedback for requests about signed packages
3. Evaluate if target users (enterprises) require signed packages

**Phase 2 (If needed):**
1. Purchase certificate when there's clear user need or compliance requirement
2. Implement signing in CI/CD pipeline
3. Announce signing in release notes

**Alternative:**
- Document that packages receive NuGet.org repository signatures (which is automatic)
- This provides some tamper protection without the cost of author signing
- Revisit author signing if user demand increases

## Additional Resources

- [Signed packages reference](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference)
- [Sign a NuGet package](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package)
- [Install and use a signed package](https://learn.microsoft.com/en-us/nuget/consume-packages/installing-signed-packages)
- [NuGet package signing requirements](https://learn.microsoft.com/en-us/nuget/reference/nuget-client-tools-version-map)
- [Code signing best practices](https://learn.microsoft.com/en-us/windows-hardware/drivers/dashboard/code-signing-best-practices)

## Questions and Answers

### Q: Do all packages on NuGet.org need to be signed?
**A:** No. Signing is optional. However, NuGet.org automatically adds a repository signature to all uploaded packages, providing some level of tamper protection even if the author doesn't sign.

### Q: What happens if our certificate expires?
**A:** If packages are properly timestamped during signing, the signatures remain valid even after certificate expiration. Without timestamping, signatures become invalid when the certificate expires.

### Q: Can we use a self-signed certificate?
**A:** Technically yes, but it defeats the purpose. NuGet clients won't trust self-signed certificates by default. You need a certificate from a trusted CA.

### Q: How do users know if a package is signed?
**A:** 
- NuGet CLI shows signature information during installation
- NuGet.org package page shows author signature status
- `dotnet nuget verify` command can check signature status

### Q: What if we change certificate providers?
**A:** You can sign new versions with a new certificate. Old versions remain signed with the old certificate. Both signatures remain valid (assuming certificates haven't been revoked).

### Q: Can we automate certificate renewal in CI/CD?
**A:** Certificate renewal itself can't be fully automated (requires interaction with CA), but you can automate updating the certificate in GitHub Secrets once renewed.

## Conclusion

NuGet package signing provides enhanced security through author identity verification and tamper protection. For the LicenseManager_12noon.Client package, signing would align well with the security-focused nature of the product and professional standards. However, the cost ($200-$500/year) and operational overhead should be weighed against current user needs.

**Recommended next steps:**
1. Survey or monitor whether target users (especially enterprise users) require or prefer signed packages
2. If there's demand, budget for certificate purchase
3. Prepare CI/CD pipeline changes before purchasing certificate
4. Consider starting with a one-year certificate to evaluate value

The implementation is straightforward and can be added to the existing `build.yml` workflow with minimal changes. The security benefits are real, but the decision should be based on user needs and budget considerations.
