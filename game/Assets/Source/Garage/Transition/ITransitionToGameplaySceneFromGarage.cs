namespace Race.Garage
{
    public interface ITransitionToGameplaySceneFromGarage
    {
        void TransitionToGameplaySceneFromGarage(CarProperties properties, UserDataModel userDataModel);
    }
}