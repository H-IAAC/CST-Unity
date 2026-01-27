namespace HIAAC.CstUnity.Core.Entities
{
    public interface Memory
    {
        object getI();

        int setI(object info);

        double getEvaluation();

        string getName();

        void setName(string name);

        void setEvaluation(double eval);

        public long getTimestamp();

        public void addMemoryObserver(MemoryObserver memoryObserver);
        public void removeMemoryObserver(MemoryObserver memoryObserver);

        public long getId();

        public void setId(long id);
    }
}