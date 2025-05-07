# Sendlix SMTP Relay

## Introduction

The Sendlix SMTP Relay is an application that acts as an intermediary between your applications and the Sendlix email sending service. It accepts SMTP connections, authenticates clients, and forwards emails via the Sendlix API.

## Installation

1.  **Prerequisites:**

    - .NET 9 or higher

2.  **Download:**

    - Download the latest version of the Sendlix SMTP Relay.

3.  **Configuration:**
    - Extract the downloaded file to a directory of your choice.
    - Configure the relay using environment variables or a configuration file (see "Configuration" section).

## Configuration

The Sendlix SMTP Relay can be configured via environment variables. All configurations are optional. Here are the available options:

- `ListenAddress`: The IP address the relay should bind to (default: `127.0.0.1`).
- `Port`: The port the relay listens for connections on (default: `587`).
- `ServerCertificatePath`: The path to an SSL certificate in PKCS12 format (optional).
- `SendlixApiKey:Username`: The username for Sendlix API authentication.
- `SendlixApiKey:ApiKey`: The API key for Sendlix API authentication.
- `SendlixApiKey:ApiKeyPath`: The path to a file containing the API key (alternative to `ApiKey`).
- `TestMode`: Enables test mode, where emails are not sent to Sendlix (default: `false`).

**Example Environment Variables:**

```bash
ListenAddress=0.0.0.0
Port=587
```

## Usage

Just run the application, and it will start listening for SMTP connections on the specified address and port. You can use any SMTP client to connect to the relay and send emails.

## Docker

You can run the Sendlix SMTP Relay in a Docker container.

``` bash
docker run -p 587:587 ghcr.io/sendlix/smtp-relay/sendlix-smtp-relay:latest
```

