namespace Foundation.Sdk.Security
{
    /// <summary>
    /// Represents the different types of tokens that can be used for OAuth2
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Opaque bearer token
        /// </summary>
        Bearer,

        /// <summary>
        /// JavaScript Web Token (JWT)
        /// </summary>
        Jwt,

        /// <summary>
        /// No token type. Use for situations where the API is not secured, such as for development purposes.
        /// </summary>
        None
    }
}