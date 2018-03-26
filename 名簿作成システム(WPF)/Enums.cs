//
//列挙型用
//
public enum DataItems { Dept, Name, Address };

public enum ItemFormat { OnlyAddress = 1 << (int)DataItems.Address, OnlyName = 1 << (int)DataItems.Name, WithoutDept = (1 << (int)DataItems.Name) | (1 << (int)DataItems.Address), All = 7, Other = 8};
