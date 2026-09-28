using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    
    public interface IAbstractFinalScore
    {

    }

    public class AbstractFinalScore : IAbstractFinalScore
    {
        public readonly EGameMode mode;

        public AbstractFinalScore(EGameMode mode)
        {
            this.mode = mode;
        }        
    }
}
