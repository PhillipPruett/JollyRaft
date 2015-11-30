namespace JollyRaft
{
    public class LogResult
    {
    }

    public class LogCommited : LogResult
    {
        public Log ServerLog { get; set; }
    }

    public class LogRejected : LogResult
    {
        public string LeaderId {get; set;}
    }

    public class LogFailed : LogResult
    {
    }
}