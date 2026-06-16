## Summary

This is an ASP.NET Web API app that demonstrates an example implementation of a JSON Web Token (JWT) authentication, including refresh token with a rotation system. It was created following the tutorial [Asp.Net Core Web API with JWT Authentication & Refresh Tokens using SQL Server + Identity Server](https://www.youtube.com/watch?v=ZBKJZyccwcE). Based on .NET 10, it uses an Entity Framework Core In-Memory database for storage and the Scalar NuGet package alongside OpenAPI for documenting the API.

## Configuration

In [`appsettings.json`](WebApiRefreshTokenDemo\appsettings.Development.json) there are some settings you need to fill. To quickly test token expiration, temporarily set shorter values (under a minute) in the `CreateRefreshToken` method in [`TokenService.cs`](WebApiRefreshTokenDemo\Services\TokenService.cs). It's advisable to match those values with the cookie expiration values in [`AuthController.cs`](WebApiRefreshTokenDemo\Controllers\AuthController.cs).

If the `CreateTestData` setting is `true`, a default user is created with these credentials:

```
UserName = "test";
Email = "test@test.test";
Password = "Test1234.";
RoleName = "Admin";
```

## How to run with Docker

In order to use Docker to run the app you need to set 2 environment variables: `DB_PASSWORD` and `CERTIFICATE_PASSWORD`. You also have to create a certificate to allow the use of a secure connection. To create the certificate run this command in a PowerShell terminal:

```
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\WebApiRefreshTokenDemo.pfx -p <password>
```

Keep in mind that this is the path in a Windows machine, and uses env susbtitution of PowerShell, if you use another OS make changes to the path and the volume in the docker-compose.yaml file accordingly. Also the password you use need to match the one in the env var.

## How to test

There is a Bruno collection in the repository root with requests pre-filled with test data. Install [Bruno](https://www.usebruno.com/downloads) or use the [Bruno extension for VS Code](https://marketplace.visualstudio.com/items?itemName=bruno-api-client.bruno) to run them. There is also a [`WebApiRefreshTokenDemo.http`](WebApiRefreshTokenDemo\WebApiRefreshTokenDemo.http) file with requests that you can use in Visual Studio or in VS Code with the [REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client).

## Endpoints

### Authentication

- `/api/auth/register` - public route; registers a new user
- `/api/auth/login` - public route; logs in a user and returns an `accessToken` cookie and a `refreshToken` cookie
- `/api/auth/refresh-token` - public route; revokes the current refresh token and returns a new access token and refresh token
- `/api/auth/revoke` - secure route; revokes the current refresh token for the user, if the current JWT has not yet expired, it remains valid until its expiration time

### Role management

- `/api/role/create` - public route; creates a new role
- `/api/role/assign` - public route; assigns a role to specified user

### Testing

- `/api/test/public` - public route; any user can access it
- `/api/test/protected` - secure route; only authenticated users can access it
- `/api/test/admin` - secure route; only authenticated users with the Admin role can access it

## Refresh token implementation

When a user requests the refresh-token endpoint, the `refreshToken` cookie is sent. The app finds that token in the database, revokes it, creates a new refresh token, generates a new access token, and returns both as cookies. Every refresh token links to its successor, so a user's refresh-token chain is auditable. A mechanism to delete old refresh tokens after a period is recommended for production but was not implemented here to keep the example simple.

The `OnMessageReceived` event of the `JwtBearerEvents` class is used to copy the refresh token from the cookie to the `HttpContext.Token` property, where the authentication system can use it.
