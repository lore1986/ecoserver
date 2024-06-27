public interface IActiveBoatTracker
{
    EcodroneBoat? CreateAddBoat(string boatId);
    EcodroneBoat? ReturnEcodroneBoatInstance(string boat_id);
    //bool RemoveEcodroneBoatInstance(string boat_id);
    void RemoveEcodroneBoatInstance(string boat_id);
}