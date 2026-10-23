# Privacy Policy for FoliCon

**Last Updated:** October 2026

FoliCon ("we", "our", or "the application") is an open-source desktop utility for Windows designed to customize folder icons for movies, TV series, anime, games, and music. This Privacy Policy explains what information FoliCon accesses, how it is used, and how your privacy is protected.

---

## 1. Information Accessed and Processed Locally

### Folder and File Metadata
- **Folder Names:** When you select folders or scan directories, FoliCon reads folder names locally on your machine to extract title and release year keywords for metadata queries.
- **Icon and Image Files:** FoliCon creates, reads, and modifies Windows folder icon files (`folder.ico`, `desktop.ini`, poster caches) solely on your local file system. FoliCon does not upload your personal files or folder contents.

### Local Settings and Preferences
- Application preferences (selected overlay styles, active themes, cache settings, language) are stored locally on your device in `%APPDATA%\FoliCon` (or the application directory).

---

## 2. Third-Party Services and Online Queries

FoliCon queries external APIs to retrieve artwork, poster images, ratings, and community overlays. Depending on the features you use, requests are sent to:

| Service | Purpose | Data Transmitted |
| :--- | :--- | :--- |
| **The Movie Database (TMDB)** | Fetch movie and TV series metadata, ratings, and posters | Search title queries and API key |
| **IGDB (Twitch / Amazon)** | Fetch video game covers and metadata | Game title queries, Client ID, OAuth access token |
| **DeviantArt** | Search and download user-created icon packs and artwork | Search queries and OAuth authorization tokens |
| **GitHub** | Check for application updates and download community themes/overlays | Standard HTTPS web requests |

These services process requests according to their own respective privacy policies. FoliCon does not sell, lease, or share your queries with advertisers.

---

## 3. Account Credentials and Token Storage

FoliCon allows you to optionally connect your own accounts or API keys for third-party providers:
- **OAuth Tokens:** For services supporting OAuth 2.0 (such as DeviantArt and Twitch/IGDB), access and refresh tokens are acquired via official authentication flows.
- **Storage Location:** All API keys and OAuth tokens are stored locally on your computer in your local user profile (`%APPDATA%\FoliCon`).
- **Security:** Credentials and tokens are used exclusively to authenticate direct API calls between your computer and the respective service provider. They are never transmitted to any FoliCon-operated server or third party.

---

## 4. Local Diagnostic Logging

FoliCon generates diagnostic log files locally using NLog to assist in troubleshooting errors and crashes:
- **Location:** Stored locally in `%APPDATA%\FoliCon\Logs`.
- **Contents:** Application events, timestamps, internal method traces, and error messages.
- Diagnostic logs remain entirely on your computer and are not uploaded automatically.

---

## 5. Crash and Error Reporting (Sentry)

FoliCon includes optional integration with [Sentry](https://sentry.io/) for crash reporting and diagnostic analytics to help diagnose unexpected software defects:
- **Data Collected:** When an unhandled exception or crash occurs, the error event may include the application version, operating system details, exception stack trace, machine name, and diagnostic breadcrumbs leading up to the error.
- **User Control:** Crash reporting can be enabled or disabled at any time through the application settings.
- **Data Retention:** Data transmitted to Sentry is retained in accordance with Sentry's data retention policies and is used solely for debugging and software stability improvements.

---

## 6. User Control, Data Deletion, and OAuth Revocation

You maintain full control over your data:
- **Disconnecting Accounts:** You can disconnect linked third-party accounts (e.g., DeviantArt) directly from FoliCon's Settings/Wizard, which removes stored tokens locally.
- **Revoking OAuth Access:** You can revoke FoliCon's access at any time through your account security settings on [DeviantArt](https://www.deviantart.com/settings/applications) or [Twitch](https://www.twitch.tv/settings/connections).
- **Clearing Stored Data:** Deleting the `%APPDATA%\FoliCon` directory will completely remove all cached posters, application configuration, diagnostic logs, and stored API credentials from your system.

---

## 7. Open Source Transparency

FoliCon is free and open-source software distributed under the GNU General Public License v3.0 (GPL-3.0). The full source code is publicly inspectable at:
[https://github.com/DineshSolanki/FoliCon](https://github.com/DineshSolanki/FoliCon)

---

## 8. Contact

If you have questions or concerns regarding this Privacy Policy, you can open an issue on GitHub:
[https://github.com/DineshSolanki/FoliCon/issues](https://github.com/DineshSolanki/FoliCon/issues)
