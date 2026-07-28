namespace Auth.Shared.Interfaces;

public interface ICorrelationIdProvider
{
    string GetCorrelationId();
}
