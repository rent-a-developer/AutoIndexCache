namespace AutoIndexCache.Tests.TestData;

public class User(Int64 id, String userName, Int64 groupId, Boolean isActive)
{
    public Int64 GroupId = groupId;
    public Int64 Id = id;
    public Boolean IsActive = isActive;
    public Object? NullableObject;
    public Nullable<Int32> NullableValue;
    public String UserName = userName;
}
