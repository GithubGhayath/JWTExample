public class RegistrationFaildException(IEnumerable<string> errorDescriptions):
Exception($"Registration faild with following errors: {string.Join(Environment.NewLine,errorDescriptions)}");