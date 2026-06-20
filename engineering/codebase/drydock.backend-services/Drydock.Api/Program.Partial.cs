// Marks the implicit top-level Program as a partial public class so the integration-test project
// can target it with WebApplicationFactory<Program>. The body stays empty — Program.cs holds the
// actual startup. (Top-level statements generate an internal Program; this opens it to the test host.)
public partial class Program;
