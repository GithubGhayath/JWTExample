public class UserAlreadyExistsException(string Email) 
:Exception($"User With Email {Email} already exists");