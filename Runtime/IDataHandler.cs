namespace HunterAllen.SaveSystem
{
    public interface IData { }
    public interface IDataSaveHandler
    {
        public void Save<T>(T t, string fileName);
        public T Load<T>(string fileName, out bool successful);
    }
    public interface IDataProvider<T>
    {
        public string Id { get; }
        /// <summary>
        /// Provides data to the save manager to be saved.
        /// </summary>
        /// <returns>This objects's save data.</returns>
        public T ProvideData();
    }
    public interface IDataHandler<T> where T : IData
    {
        public string Id { get; }
        /// <summary>
        /// Called once data of type T is loaded.
        /// </summary>
        /// <param name="data"></param>
        public void HandleData(IData data) => HandleData((T)data);
        /// <summary>
        /// Called once data of type T is loaded.
        /// </summary>
        /// <param name="data"></param>
        public void HandleData(T data);
    }
}