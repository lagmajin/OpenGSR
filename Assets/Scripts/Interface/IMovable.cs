namespace OpenGS
{
    public interface IMovable
    {
        void Jump();
        void LieDown();

        bool Sitting();
        void Sit();
    }
}
