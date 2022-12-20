public class EmailModelCreator : IEmailModelCreator 
{ 
    private const string PreviewTextKey = "PreviewText"; 
        
    private readonly IEnumerable<IEmailModelBuilder> modelBuilders; 
    private readonly IVariableProcessor variableProcessor; 

    public EmailModelCreator(IEnumerable<IEmailModelBuilder> modelBuilders, IVariableProcessor variableProcessor) 
    { 
        this.modelBuilders = modelBuilders; 
        this.variableProcessor = variableProcessor; 
    } 

    [Trace]
    public async Task<dynamic> CreateModelAsync(RenderEmailModel emailModel) 
    { 
        IDictionary<string, object>[] models = await Task.WhenAll( 
            this.modelBuilders.Select(builder => builder.BuildModelAsync(emailModel))); 

        var resultModels = new Dictionary<string, object>(); 
        foreach (var model in models.SelectMany(m => m)) 
        { 
            resultModels[model.Key] = model.Value; 
        } 
            
        resultModels[PreviewTextKey] = this.variableProcessor.Process(emailModel.PreviewText, resultModels); 
        return resultModels; 
    } 
}


public class AccountModelBuilder : IEmailModelBuilder 
{ 
    private readonly IAccountRepository accountRepository; 

    public AccountModelBuilder(IAccountRepository accountRepository) 
    { 
        this.accountRepository = accountRepository; 
    } 

    public async Task<IDictionary<string, object>> BuildModelAsync(RenderEmailModel emailModel) 
    { 
        dynamic result = new ExpandoObject(); 

        if (!int.TryParse(emailModel.User?.AccountId, out int accountId)) 
        { 
            return result; 
        } 

        var account = await this.accountRepository.GetAsync(accountId); 
        result.Account = account; 
        result.AccountName = account?.Name; 
        result.AccountLogo = account?.LogoUri; 
        result.AccountId = accountId; 
        return result; 
    } 
}

public class SenderInfoModelBuilder : IEmailModelBuilder 
{ 
    private readonly IAccountRepository accountRepository; 
    private readonly IUserRepository userRepository; 

    public SenderInfoModelBuilder(IAccountRepository accountRepository, IUserRepository userRepository) 
    { 
        this.accountRepository = accountRepository; 
        this.userRepository = userRepository; 
    } 

    public async Task<IDictionary<string, object>> BuildModelAsync(RenderEmailModel emailModel) 
    { 
        dynamic result = new ExpandoObject(); 
            
        if (emailModel.Sender.FromUserId == null || emailModel.Sender.FromAccountId == null) 
        { 
            return result; 
        } 

        await this.PopulateUserModel(emailModel.Sender, result); 
        await this.PopulateAccountModel(emailModel.Sender, result); 

        return result; 
    } 

    private async Task PopulateAccountModel(ISender sender, dynamic result) 
    { 
        var account = await this.accountRepository.GetAsync(sender.FromAccountId!.Value); 
        result.FromAccount = account; 
        result.FromAccountName = account?.Name; 
        result.FromAccountLogo = account?.LogoUri; 
        result.FromAccountId = sender.FromAccountId; 
    } 

    private async Task PopulateUserModel(ISender sender, dynamic result) 
    { 
        var user = await this.userRepository.GetAsync(sender.FromAccountId!.Value, sender.FromUserId!.Value); 
        result.FromUser = user; 
        result.FromFirstName = user?.FirstName; 
        result.FromLastName = user?.LastName; 
        result.FromEmail = user?.Email; 
        result.FromUserId = user?.UserId; 
        result.FromDepartment = user?.Department; 
        result.FromTitle = user?.JobTitle; 
    } 
}