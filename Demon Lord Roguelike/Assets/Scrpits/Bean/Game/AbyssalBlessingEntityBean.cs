public class AbyssalBlessingEntityBean
{
    public string abyssalBlessingUUID;
    public AbyssalBlessingInfoBean abyssalBlessingInfo;

    public AbyssalBlessingEntityBean(AbyssalBlessingInfoBean abyssalBlessingInfo)
    { 
        this.abyssalBlessingInfo = abyssalBlessingInfo;
        this.abyssalBlessingUUID = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
    }
    
}