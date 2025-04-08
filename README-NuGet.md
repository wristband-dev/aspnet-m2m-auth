# Wristband Machine-toMachine (M2M) Auth SDK for ASP.NET

Wristband provides enterprise-ready auth that is secure by default, truly multi-tenant, and ungated for small businesses.

- Website: [Wristband Website](https://wristband.dev)
- Documentation: [Wristband Docs](https://docs.wristband.dev/)

For detailed setup instructions and usage guidelines, visit the project's GitHub repository.

- [ASP.NET M2M Auth SDK - GitHub](https://github.com/wristband-dev/aspnet-m2m-auth)


This SDK can be used by Wristband machine-to-machine clients to retrieve an access token. The access token is cached in memory for subsequent calls. When the access token expires, the SDK will automatically get a new access token. The cached access token is tied to an instance of a `WristbandM2MClient`. Therefore, it's optimal to create a single instance
of the `WristbandM2MClient` so that the access token cache will be utilized globally.

## Details

This SDK facilitates seamless interaction with Wristband for machine-to-machine (M2M) authentication within multi-tenant ASP.NET Core applications. It follows OAuth 2.1 and OpenID standards and is supported for .NET 6+. Key functionalities allow for the following:

- Acquiring an access token on server startup for a M2M OAuth2 client
- How to protect an API with access tokens
- How to refresh the access tokens for the M2M OAuth2 client.

- Initiating a login request by redirecting to Wristband.
- Receiving callback requests from Wristband to complete a login request.
- Retrieving all necessary JWT tokens and userinfo to start an application session.
- Logging out a user from the application by revoking refresh tokens and redirecting to Wristband.
- Checking for expired access tokens and refreshing them automatically, if necessary.

You can learn more about how authentication works in Wristband in our documentation:

- [Auth Flows Walkthrough](https://docs.wristband.dev/docs/auth-flows-and-diagrams)
- [Login Workflow In Depth](https://docs.wristband.dev/docs/login-workflow)

## Questions

Reach out to the Wristband team at <support@wristband.dev> for any questions regarding this SDK.
