# NuGet Package Signing Guide

This document provides a comprehensive guide to implementing NuGet package signing for the LicenseManager_12noon.Client package.

## Table of Contents

1. [What is NuGet Package Signing?](#what-is-nuget-package-signing)
2. [Security Benefits](#security-benefits)
3. [Advantages](#advantages)
4. [Disadvantages and Considerations](#disadvantages-and-considerations)
5. [Implementation Steps](#implementation-steps)
6. [CI/CD Integration (GitHub Actions)](#cicd-integration-github-actions)
7. [Certificate Management](#certificate-management)
8. [Testing and Verification](#testing-and-verification)
9. [Resources](#resources)

## What is NuGet Package Signing?

NuGet package signing is a security practice where a cryptographic signature—backed by a certificate from a trusted authority—is applied to NuGet packages (.nupkg). The signature ensures that:

- The package has not been tampered with since it was signed (integrity)
- The package comes from a verified publisher (authenticity)
- The signing entity cannot deny having signed the package (non-repudiation)

## Security Benefits

### How is it More Secure?

1. **Integrity Verification**
   - Detects any unauthorized changes to the package after signing
   - Prevents malware injection and supply chain attacks
   - Ensures the package content matches what the publisher released

2. **Authenticity & Provenance**
   - Proves the package origin and publisher identity
   - Reduces risk of typosquatting and package impersonation
   - Enables organizations to enforce trusted publisher policies

3. **Non-Repudiation**
   - Creates an irrefutable record of who published a specific package version
   - Important for auditing, compliance, and dispute resolution

4. **Supply Chain Attack Mitigation**
   - Critical in 2024 as supply chain attacks on dependencies remain a major threat
   - Organizations can configure their build pipelines to only accept signed packages
   - Reduces the risk of consuming malicious or compromised packages

## Advantages

1. **Increased Trust and Reputation**
   - Builds confidence among package consumers
   - Important for enterprise and regulated environments
   - NuGet.org may prioritize signed packages in search and recommendations

2. **Policy Enforcement**
   - Enables organizations to require signature verification before package installation
   - Supports compliance requirements and security policies

3. **Enhanced Security Posture**
   - Demonstrates commitment to software supply chain security
   - Part of modern DevSecOps best practices

4. **Audit Trail**
   - Provides verifiable record of package releases
   - Helps with compliance and security audits

## Disadvantages and Considerations

1. **Certificate Procurement and Cost**
   - Requires purchasing a code signing certificate from a trusted Certificate Authority (CA)
   - Typical providers: DigiCert, GlobalSign, Sectigo (formerly Comodo), SSL.com, Certum
   - Cost ranges from $100-$500+ per year depending on the CA and certificate type
   - Extended Validation (EV) certificates cost more but provide higher trust levels

2. **Certificate Management Complexity**
   - Requires secure storage and handling of private keys
   - Certificate renewals must be managed (typically annual)
   - Revocation processes needed if certificate is compromised
   - Team access and backup procedures must be established

3. **Process Overhead**
   - Build and release pipelines must be updated
   - Additional steps in the CI/CD workflow
   - Requires proper timestamping to ensure signatures remain valid after certificate expiration
   - May slightly increase build times

4. **Compatibility and Tooling**
   - Different certificate formats (PFX, PEM) may require different handling
   - Hardware Security Modules (HSM) or USB tokens add complexity
   - Not all development environments may support all certificate types
   - Cross-platform considerations (Windows vs. Linux runners)

5. **User Configuration Required**
   - Package consumers must properly configure signature validation
   - Users may ignore or misunderstand signature warnings
   - Does not automatically prevent installation of unsigned packages

6. **Limited Protection Scope**
   - Signing does NOT guarantee the package is free from vulnerabilities
   - Malicious code can still be signed (if the signing account is compromised)
   - Should be combined with code reviews, security scans, and vulnerability checks

## Implementation Steps

### Prerequisites

1. **Obtain a Code Signing Certificate**
   - Purchase from a trusted CA (DigiCert, GlobalSign, Sectigo, SSL.com, etc.)
   - Download the certificate in PFX format (includes private key)
   - Note the certificate password for secure storage

2. **Test Certificate Locally**
   ```bash
   # Verify certificate is valid
   openssl pkcs12 -info -in certificate.pfx
   ```

### Required Changes to the Repository

The following changes would be needed to implement package signing:

#### 1. Update Project File (LicenseManager_12noon.Client.csproj)

Currently, the project file does not include signing configuration. No changes are needed to the .csproj file itself, as signing is done during the pack/publish step using the `dotnet nuget sign` command.

#### 2. Update GitHub Actions Workflow (.github/workflows/build.yml)

The current workflow needs modifications in the `release` job to add package signing. The changes would be inserted after the package is created but before it's pushed to NuGet.org.

**Current flow:**
1. Build package
2. Push to NuGet.org

**Updated flow:**
1. Build package
2. **Sign package** (NEW)
3. Push to NuGet.org

#### 3. Add GitHub Secrets

The following secrets must be added to the GitHub repository settings:

- `CODE_SIGN_PFX`: Base64-encoded certificate file
- `CODE_SIGN_PASSWORD`: Certificate password
- `API_KEY_NUGET`: Already exists for publishing

### Detailed Workflow Changes

The following steps would be added to the `release` job in `.github/workflows/build.yml`:

```yaml
# Add after the "📥 Download deployment folder" step
# and before the "🚀 Publish NuGet package" step

- name: 🔐 Decode and prepare signing certificate
  if: ${{ startsWith(github.ref, 'refs/tags/v') && (steps.tag_on_main.outputs.on_main == 'true') }}
  run: |
    # Decode the base64-encoded certificate
    echo "${{ secrets.CODE_SIGN_PFX }}" | base64 --decode > cert.pfx
    
    # Set appropriate permissions (Linux/Ubuntu runner)
    chmod 600 cert.pfx

- name: ✍️ Sign NuGet package
  if: ${{ startsWith(github.ref, 'refs/tags/v') && (steps.tag_on_main.outputs.on_main == 'true') }}
  run: |
    # Sign the package
    dotnet nuget sign ./release/*.nupkg \
      --certificate-path cert.pfx \
      --certificate-password "${{ secrets.CODE_SIGN_PASSWORD }}" \
      --timestamper http://timestamp.digicert.com \
      --overwrite
    
    # Optionally verify the signature
    dotnet nuget verify ./release/*.nupkg --all

- name: 🧹 Clean up certificate file
  if: ${{ always() && startsWith(github.ref, 'refs/tags/v') && (steps.tag_on_main.outputs.on_main == 'true') }}
  run: |
    # Securely remove the certificate file
    rm -f cert.pfx
```

**Key Points:**

1. **Base64 Encoding**: The certificate must be base64-encoded before storing as a GitHub secret:
   ```bash
   base64 -i certificate.pfx -o certificate.pfx.base64
   # On Windows PowerShell:
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("certificate.pfx")) > certificate.pfx.base64
   ```

2. **Timestamping**: Essential for signature validity after certificate expiration
   - DigiCert timestamper: `http://timestamp.digicert.com`
   - Alternative: `http://timestamp.sectigo.com`
   - If certificate expires, the timestamp proves the signature was valid at signing time

3. **Runner Considerations**: The current workflow uses `ubuntu-latest` for the release job, which is compatible with `dotnet nuget sign` command

4. **Security**: The certificate file is cleaned up in an `always()` condition to ensure it's removed even if signing fails

## CI/CD Integration (GitHub Actions)

### Current Build Process

The current `build.yml` workflow:
1. Builds the project on `windows-latest` runner
2. Runs unit tests
3. Packs the NuGet package (if version tag)
4. Uploads artifacts
5. Creates GitHub release (separate job on `ubuntu-latest`)
6. Publishes to NuGet.org (if on main branch)

### Impact of Adding Signing

**Minimal Impact:**
- Adds 2-3 additional steps to the release job
- Increases build time by approximately 5-15 seconds (signing + verification)
- No impact on build or test jobs
- No changes to manual dispatch or non-tag builds

**Security Improvements:**
- Package integrity guaranteed
- Publisher authenticity verified
- Enhanced trust for package consumers

**Compatibility:**
- Works with existing `ubuntu-latest` runner
- Compatible with current .NET SDK setup
- No changes needed to other jobs

## Certificate Management

### Secure Storage Options

1. **GitHub Secrets (Recommended for this project)**
   - Encrypted at rest
   - Only accessible during workflow runs
   - Can be scoped to specific environments
   - Easy to rotate

2. **Azure Key Vault** (Alternative for enterprise)
   - Centralized certificate management
   - HSM-backed storage available
   - Audit logging
   - Requires Azure subscription and additional configuration

3. **Hardware Security Module (HSM)** (Enterprise option)
   - Highest security level
   - Private key never leaves the device
   - More complex integration
   - Higher cost

### Certificate Renewal Process

1. **Before Expiration:**
   - Purchase renewed certificate from CA (typically 30-60 days before expiration)
   - Download new certificate in PFX format
   - Base64-encode the new certificate
   - Update `CODE_SIGN_PFX` secret in GitHub repository settings
   - Update `CODE_SIGN_PASSWORD` secret if password changed

2. **Testing:**
   - Create a test tag to trigger a draft release
   - Verify the package is signed with the new certificate
   - Check signature validity

3. **Old Certificate:**
   - Keep the old certificate for reference until all packages are re-signed
   - Do NOT delete old certificate immediately

### Best Practices

1. **Certificate Backup:**
   - Store certificate backup in a secure, offline location
   - Use encrypted storage (e.g., password-protected archive)
   - Document the backup location

2. **Access Control:**
   - Limit who can view/edit repository secrets
   - Use GitHub environment protection rules for production releases
   - Maintain an audit log of certificate access

3. **Rotation:**
   - Plan for certificate renewal 2-3 months in advance
   - Test renewal process in a non-production environment first
   - Update documentation with renewal dates

## Testing and Verification

### Verify Signed Package

After signing, verify the package signature:

```bash
# Verify all signatures
dotnet nuget verify MyPackage.1.0.0.nupkg --all

# Verify specific signature
dotnet nuget verify MyPackage.1.0.0.nupkg --certificate-fingerprint <fingerprint>
```

### Consumer Configuration

Package consumers can configure signature validation in their `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <trustedSigners>
    <author name="12noon LLC">
      <certificate fingerprint="YOUR_CERTIFICATE_FINGERPRINT"
                   hashAlgorithm="SHA256"
                   allowUntrustedRoot="false" />
    </author>
  </trustedSigners>
</configuration>
```

### CI/CD Testing

1. **Test on a Branch:**
   - Create a test branch (e.g., `test-signing`)
   - Create a test version tag (e.g., `v1.2.3-test`)
   - Push the tag to trigger the workflow
   - Verify the draft release contains a signed package

2. **Local Testing:**
   ```bash
   # Build package
   dotnet pack LicenseManager_12noon.Client/LicenseManager_12noon.Client.csproj \
     --configuration Release --output ./release/

   # Sign package
   dotnet nuget sign ./release/*.nupkg \
     --certificate-path certificate.pfx \
     --certificate-password "your-password" \
     --timestamper http://timestamp.digicert.com

   # Verify signature
   dotnet nuget verify ./release/*.nupkg --all
   ```

## Resources

### Official Documentation

- [Microsoft: Signed Packages Reference](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference)
- [Microsoft: Sign a NuGet Package](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package)
- [NuGet Package Signing](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package)
- [dotnet nuget sign command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-sign)
- [dotnet nuget verify command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-verify)

### Certificate Authorities

- [DigiCert Code Signing](https://www.digicert.com/signing/code-signing-certificates)
- [Sectigo Code Signing](https://sectigo.com/ssl-certificates-tls/code-signing)
- [GlobalSign Code Signing](https://www.globalsign.com/en/code-signing-certificate)
- [SSL.com Code Signing](https://www.ssl.com/certificates/code-signing/)
- [Certum Code Signing](https://www.certum.eu/en/code-signing-certificates/)

### Additional Reading

- [NuGet Package Security Best Practices](https://www.meziantou.net/nuget-packages-security-risks-and-best-practices.htm)
- [Encryption Consulting: All About NuGet Signing](https://www.encryptionconsulting.com/all-you-need-to-know-about-nuget-signing/)
- [Publishing NuGet Packages with Trusted Publishing](https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/)

## Summary

### To Implement Package Signing:

1. **Purchase Certificate** ($100-$500/year)
   - Choose a trusted CA
   - Get certificate in PFX format

2. **Add Secrets to GitHub**
   - Base64-encode certificate: `CODE_SIGN_PFX`
   - Store password: `CODE_SIGN_PASSWORD`

3. **Update Workflow** (`.github/workflows/build.yml`)
   - Add signing step after package creation
   - Add verification step
   - Add cleanup step

4. **Test**
   - Create test tag on non-main branch
   - Verify draft release has signed package
   - Validate signature

5. **Document**
   - Update README if desired
   - Document certificate renewal process
   - Train team on certificate management

### Recommended Next Steps:

1. **Evaluate Cost vs. Benefit**
   - Determine if the security benefits justify the annual certificate cost
   - Consider the target audience and their security requirements

2. **Choose Certificate Authority**
   - Compare prices and features
   - Consider EV vs. Standard certificates

3. **Plan Implementation**
   - Schedule certificate purchase
   - Allocate time for workflow updates
   - Plan testing approach

4. **Gradual Rollout**
   - Test on non-production tags first
   - Verify with small group of users
   - Monitor for issues

---

**Note:** This document provides a comprehensive guide for implementing NuGet package signing. The actual implementation would require purchasing a certificate and making the described changes to the repository configuration and GitHub Actions workflow. The changes are minimal and focused, primarily affecting the release job in the CI/CD pipeline.
