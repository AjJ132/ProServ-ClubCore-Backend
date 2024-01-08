using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProServ_ClubCore_Server_API.Database;
using ProServ_ClubCore_Server_API.DTO;
using ProServ_ClubCore_Server_API.Models;

namespace ProServ_ClubCore_Server_API.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDbContextFactory<ProServDbContext> _contextFactory;

        public MessageController(UserManager<IdentityUser> userManager, IDbContextFactory<ProServDbContext> contextFactory)
        {
            _userManager = userManager;
            _contextFactory = contextFactory;
        }

        private async Task<bool> ValidateUserDirectConversationAccess(Guid conversationId, string userId)
        {
            try
            {
                using (var context = _contextFactory.CreateDbContext())
                {
                    var conversation = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.Conversation_ID == conversationId);

                    if (conversation == null)
                    {

                        return false;
                    }

                    if (conversation.User1_ID != userId && conversation.User2_ID != userId)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                //TODO log error
                return false;
            }
        }

        private async Task<bool> ValidateUserGroupConversationAccess(Guid conversationId, string userId)
        {
            try
            {
                using (var context = _contextFactory.CreateDbContext())
                {
                    var conversation = await context.GroupConversations.FirstOrDefaultAsync(gc => gc.Conversation_ID == conversationId);

                    if (conversation == null)
                    {
                        return false;
                    }

                    //validate user is part of group conversation
                    var userInGroupConversation = await context.ConversationUsers.FirstOrDefaultAsync(cu => cu.Conversation_ID == conversationId && cu.User_ID == userId);

                    if (userInGroupConversation == null)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                //TODO log error
                return false;
            }
        }

        private string FormatTimestamp(DateTimeOffset timestamp)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            if (timestamp.Date == today)
            {
                // Format as time only for today
                return timestamp.ToString("hh:mm");
            }
            else if (timestamp >= startOfWeek)
            {
                // Format as day of the week for dates within this week
                return timestamp.ToString("dddd");
            }
            else
            {
                // Format as a full date for older dates
                return timestamp.ToString("yyyy-MM-dd");
            }
        }


        [HttpGet("get-users-to-message")]
        [Authorize]
        public async Task<IActionResult> GetUsersToMessage()
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }


                using (var context = _contextFactory.CreateDbContext())
                {
                    //get curent user
                    var currentUserEntity = await context.Users.FirstOrDefaultAsync(u => u.User_ID == currentUser.Id);

                    if (currentUserEntity == null)
                    {
                        return Unauthorized("User was not found");
                    }

                    //check team ID
                    if (currentUserEntity.Team_ID == null)
                    {
                        return Ok("");
                    }

                    //get users in team
                    var usersInTeam = await context.Users.Where(u => u.Team_ID == currentUserEntity.Team_ID).ToListAsync();

                    //remove current user from list
                    usersInTeam.RemoveAll(u => u.User_ID == currentUser.Id);

                    //convert users to DTO
                    var usersInTeam_DTO = new List<UserLookup_DTO>();
                    foreach (var user in usersInTeam)
                    {
                        var teamMember = new UserLookup_DTO
                        {
                            User_ID = user.User_ID,
                            Name = user.First_Name + " " + user.Last_Name
                        };

                        usersInTeam_DTO.Add(teamMember);
                    }

                    return Ok(usersInTeam_DTO);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("get-my-message-threads")]
        [Authorize]
        public async Task<IActionResult> GetMyMessageThreads()
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get conversations
                using (var context = _contextFactory.CreateDbContext())
                {
                    List<UniversalConversations_DTO> universalConversations_DTO = new List<UniversalConversations_DTO>();

                    //get direct conversations
                    var directConversations = await context.DirectConversations.Where(dc => dc.User1_ID == currentUser.Id || dc.User2_ID == currentUser.Id).ToListAsync();

                    //get group conversations
                    //grab any if user was a creator
                    var groupConversations = await context.GroupConversations.Where(gc => gc.Creator_ID == currentUser.Id).ToListAsync();

                    //next check conversation users to find all group messages they are apart of
                    var gcUsersConversationIDs = await context.ConversationUsers.Where(cu => cu.User_ID == currentUser.Id).Select(cu => cu.Conversation_ID).ToListAsync();

                    //get group conversations
                    var groupConversations2 = await context.GroupConversations.Where(gc => gcUsersConversationIDs.Contains(gc.Conversation_ID)).ToListAsync();

                    //merge group conversations remove duplicates
                    groupConversations.AddRange(groupConversations2);

                    //remove duplicates
                    groupConversations = groupConversations.Distinct().ToList();

                    foreach (var conversation in directConversations)
                    {
                        UniversalConversations_DTO dCDTO = new UniversalConversations_DTO();
                        dCDTO.Conversation_ID = conversation.Conversation_ID;
                        dCDTO.Conversation_Type = 0;

                        //get other user id. Where not equal to current user id
                        string otherUserId = conversation.User1_ID == currentUser.Id ? conversation.User2_ID : conversation.User1_ID;

                        dCDTO.Conversation_Title = await context.Users.Where(u => u.User_ID == otherUserId).Select(u => u.First_Name + " " + u.Last_Name).FirstOrDefaultAsync();

                        //get last message
                        var lastMessage = await context.DirectMessages.Where(dm => dm.Conversation_ID == conversation.Conversation_ID).OrderByDescending(dm => dm.Timestamp).FirstOrDefaultAsync();

                        if (lastMessage == null)
                        {
                            dCDTO.LastMessageTimestamp = null;
                        }
                        else
                        {
                            dCDTO.LastMessageTimestamp = FormatTimestamp(lastMessage.Timestamp);
                        }

                        if (lastMessage == null)
                        {
                            dCDTO.hasUnreadMessages = false;
                        }
                        else
                        {
                            dCDTO.hasUnreadMessages = lastMessage.Sender_ID != currentUser.Id && !lastMessage.Seen;
                        }

                        //add to list
                        universalConversations_DTO.Add(dCDTO);
                    }

                    //convert group conversations to DTO
                    foreach (var conversation in groupConversations)
                    {
                        UniversalConversations_DTO gCDTO = new UniversalConversations_DTO();
                        gCDTO.Conversation_Type = 1;
                        gCDTO.Conversation_ID = conversation.Conversation_ID;

                        //get group name
                        gCDTO.Conversation_Title = conversation.Title;

                        //get last message
                        var lastMessage = await context.GroupConversationMessages.Where(gm => gm.Conversation_ID == conversation.Conversation_ID).OrderByDescending(gm => gm.Timestamp).FirstOrDefaultAsync();

                        if (lastMessage == null)
                        {
                            gCDTO.LastMessageTimestamp = null;
                        }
                        else
                        {
                            gCDTO.LastMessageTimestamp = FormatTimestamp(lastMessage.Timestamp);
                        }

                        //check GroupConversationUserSeenStatus for seen status
                        var seenStatus = await context.GroupConversationUserSeenStatuses.FirstOrDefaultAsync(gcuss => gcuss.GroupConversation_ID == conversation.Conversation_ID && gcuss.User_ID == currentUser.Id);

                        if (seenStatus == null)
                        {
                            gCDTO.hasUnreadMessages = false;
                        }
                        else
                        {
                            gCDTO.hasUnreadMessages = !seenStatus.Seen;
                        }

                        //add to list
                        universalConversations_DTO.Add(gCDTO);
                    }

                    return Ok(universalConversations_DTO);
                }


            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("get-conversation-members")]
        [Authorize]
        public async Task<IActionResult> GetConversationUsers([FromQuery] Guid conversationID, [FromQuery] int conversationType)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                using (var context = _contextFactory.CreateDbContext())
                {
                    if (conversationType == 0)
                    {
                        //get direct conversation users
                        var directConversation = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.Conversation_ID == conversationID);

                        if (directConversation == null)
                        {
                            return BadRequest("Direct conversation was not found");
                        }

                        var user1 = await context.Users.FirstOrDefaultAsync(u => u.User_ID == directConversation.User1_ID);
                        var user2 = await context.Users.FirstOrDefaultAsync(u => u.User_ID == directConversation.User2_ID);

                        if (user1 == null || user2 == null)
                        {
                            return BadRequest("One or more users were not found");
                        }

                        List<UserLookup_DTO> users =
                        [
                            new UserLookup_DTO
                            {
                                User_ID = user1.User_ID,
                                Name = user1.First_Name + " " + user1.Last_Name
                            },
                            new UserLookup_DTO
                            {
                                User_ID = user2.User_ID,
                                Name = user2.First_Name + " " + user2.Last_Name
                            },
                        ];

                        return Ok(users);
                    }
                    else if (conversationType == 1)
                    {
                        //get group conversation users
                        var groupConversation = await context.GroupConversations.FirstOrDefaultAsync(gc => gc.Conversation_ID == conversationID);


                        if (groupConversation == null)
                        {
                            return BadRequest("Group conversation was not found");
                        }

                        //get all users in group conversation
                        var groupConversationUsers = await context.ConversationUsers.Where(cu => cu.Conversation_ID == conversationID).ToListAsync();

                        //convert to DTO
                        List<UserLookup_DTO> users = new List<UserLookup_DTO>();

                        foreach (var user in groupConversationUsers)
                        {
                            var userEntity = await context.Users.FirstOrDefaultAsync(u => u.User_ID == user.User_ID);

                            if (userEntity == null)
                            {
                                continue; //skip user, TODO log error
                            }

                            users.Add(new UserLookup_DTO
                            {
                                User_ID = userEntity.User_ID,
                                Name = userEntity.First_Name + " " + userEntity.Last_Name
                            });
                        }

                        return Ok(users);
                    }
                    else
                    {
                        return BadRequest("Invalid conversation type");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //create new direct message thread
        [HttpPost("Direct/new-direct-message-thread")]
        [Authorize]
        public async Task<IActionResult> CreateNewDirectMessageThread([FromBody] DirectConversation_DTO newDC_DTO)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get user2
                var user2 = await _userManager.FindByIdAsync(newDC_DTO.User2_ID);

                if (user2 == null)
                {
                    return BadRequest("User2 was not found");
                }

                //save new direct conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    //ensure there is not already a direct conversation between these two users
                    var existingDC = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.User1_ID == currentUser.Id && dc.User2_ID == user2.Id);

                    if (existingDC != null)
                    {
                        return StatusCode(StatusCodes.Status409Conflict, "DC Already Exists");
                    }

                    //create new direct conversation
                    var newDC_Entity = new DirectConversation
                    {
                        Conversation_ID = Guid.NewGuid(),
                        User1_ID = currentUser.Id,
                        User2_ID = user2.Id,
                        Date_Created = DateTimeOffset.UtcNow
                    };

                    await context.DirectConversations.AddAsync(newDC_Entity);
                    await context.SaveChangesAsync();

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //create new group message
        [HttpPost("Group/new-group-message-thread")]
        [Authorize]
        public async Task<IActionResult> CreateNewGroupMessageThread([FromBody] NewGroupConversation_DTO newGC_DTO)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                using (var context = _contextFactory.CreateDbContext())
                {
                    GroupConversation_DTO groupConversation_DTO = null;

                    var strategy = context.Database.CreateExecutionStrategy();
                    _ = await strategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = context.Database.BeginTransaction()) //using transaction to ensure all changes are rolled back if an error occurs. Incase a group conversation is created but not all users are added
                        {
                            try
                            {
                                var creator = await context.Users.FirstOrDefaultAsync(u => u.User_ID == currentUser.Id);
                                if (creator == null)
                                {
                                    throw new Exception("Creator not found in database");
                                }

                                if (newGC_DTO.GroupName == null || newGC_DTO.GroupName == "")
                                {
                                    throw new Exception("Group name cannot be empty");
                                }

                                var newGroupConversation = new GroupConversation
                                {
                                    Conversation_ID = Guid.NewGuid(),
                                    Title = newGC_DTO.GroupName,
                                    Date_Created = DateTimeOffset.UtcNow,
                                    Creator_ID = currentUser.Id,
                                    Group_Type = 1 // Assuming this is your business logic
                                };

                                await context.GroupConversations.AddAsync(newGroupConversation);

                                Dictionary<string, ConversationUsers> userKVP = new Dictionary<string, ConversationUsers>();
                                List<GroupConversationUserSeenStatus> groupConversationUserSeenStatuses = new List<GroupConversationUserSeenStatus>();
                                foreach (var userID in newGC_DTO.User_IDs)
                                {
                                    var user = await context.Users.FirstOrDefaultAsync(u => u.User_ID == userID);
                                    if (user == null)
                                    {
                                        throw new Exception("User not found in database");
                                    }

                                    if (user.Team_ID != creator.Team_ID)
                                    {
                                        throw new Exception("User not in the same team as creator");
                                    }

                                    var conversationUser = new ConversationUsers
                                    {
                                        Conversation_ID = newGroupConversation.Conversation_ID,
                                        User_ID = userID
                                    };

                                    userKVP.Add(user.First_Name + " " + user.Last_Name, conversationUser);

                                    //create new entity in GroupConversationUserSeenStatus
                                    var groupConversationUserSeenStatus = new GroupConversationUserSeenStatus
                                    {
                                        GroupConversation_ID = newGroupConversation.Conversation_ID,
                                        User_ID = userID,
                                        Seen = true
                                    };

                                    groupConversationUserSeenStatuses.Add(groupConversationUserSeenStatus);
                                }

                                //add creator to conversation users list
                                var creatorConversationUser = new ConversationUsers
                                {
                                    Conversation_ID = newGroupConversation.Conversation_ID,
                                    User_ID = currentUser.Id
                                };

                                userKVP.Add(creator.First_Name + " " + creator.Last_Name, creatorConversationUser);

                                //add creator to GroupConversationUserSeenStatus
                                var creatorGroupConversationUserSeenStatus = new GroupConversationUserSeenStatus
                                {
                                    GroupConversation_ID = newGroupConversation.Conversation_ID,
                                    User_ID = currentUser.Id,
                                    Seen = true
                                };

                                groupConversationUserSeenStatuses.Add(creatorGroupConversationUserSeenStatus);

                                await context.GroupConversationUserSeenStatuses.AddRangeAsync(groupConversationUserSeenStatuses);
                                await context.ConversationUsers.AddRangeAsync(userKVP.Values);
                                await context.SaveChangesAsync();

                                groupConversation_DTO = new GroupConversation_DTO
                                {
                                    Conversation_ID = newGroupConversation.Conversation_ID,
                                    Conversation_Type = 1,
                                    Creator_Name = creator.First_Name + " " + creator.Last_Name,
                                    GroupName = newGroupConversation.Title,
                                    LastMessageTimestamp = null,
                                    hasUnreadMessages = false,
                                    Users = new Dictionary<string, string>()
                                };

                                transaction.Commit(); //important to commit changes to database

                                foreach (var user in userKVP)
                                {
                                    groupConversation_DTO.Users.Add(user.Key, user.Value.User_ID);
                                }

                                return Ok(groupConversation_DTO);
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync(); //important to rollback if exception occurs and remove database clutter
                                throw ex;
                            }
                        }
                    });

                    if (groupConversation_DTO != null)
                    {
                        return Ok(groupConversation_DTO);
                    }
                    else
                    {
                        return BadRequest("An error occurred while creating the group conversation");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //get my message threads

        //get messages for a direct conversation
        [HttpGet("Direct/{conversationID}/messages")]
        [Authorize]
        public async Task<IActionResult> GetMessagesForDirectMessageThread([FromQuery] Guid conversationID, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get direct conversation
                using (var context = _contextFactory.CreateDbContext())
                {

                    bool valid = await ValidateUserDirectConversationAccess(conversationID, currentUser.Id);

                    if (!valid)
                    {
                        return Unauthorized("You are not part of this direct conversation");
                    }

                    var directConversation = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.Conversation_ID == conversationID);

                    //get messages for direct conversation
                    //var messages = await context.DirectMessages.Where(dm => dm.Conversation_ID == conversationID).OrderByDescending(dm => dm.Timestamp).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
                    var messages = await context.DirectMessages.Where(dm => dm.Conversation_ID == conversationID).OrderByDescending(dm => dm.Timestamp).ToListAsync();
                    var otherUser = await context.Users.FirstOrDefaultAsync(u => u.User_ID == (directConversation.User1_ID == currentUser.Id ? directConversation.User2_ID : directConversation.User1_ID));

                    //convert messages to DTO
                    var messages_DTO = new List<DirectMessage_DTO>();

                    foreach (var message in messages)
                    {
                        messages_DTO.Add(new DirectMessage_DTO
                        {
                            Conversation_ID = message.Conversation_ID,
                            Sender_ID = message.Sender_ID,
                            Sender_Name = otherUser.First_Name + " " + otherUser.Last_Name,
                            Message = message.Message,
                            Timestamp = message.Timestamp,
                            Seen = message.Seen

                        });
                    }

                    return Ok(messages_DTO);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        //send message to a thread
        [HttpPost("Direct/{conversationID}/send-message")]
        [Authorize]
        public async Task<IActionResult> SendMessageToDirectThread([FromQuery] Guid conversationID, [FromBody] string message)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get direct conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    bool valid = await ValidateUserDirectConversationAccess(conversationID, currentUser.Id);

                    if (!valid)
                    {
                        return Unauthorized("You are not part of this direct conversation");
                    }

                    var directConversation = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.Conversation_ID == conversationID);

                    //create new direct message
                    var newDirectMessage = new DirectMessage
                    {
                        Conversation_ID = conversationID,
                        Sender_ID = currentUser.Id,
                        Message = message,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    //save new direct message
                    await context.DirectMessages.AddAsync(newDirectMessage);
                    await context.SaveChangesAsync();

                    var user = await context.Users.FirstOrDefaultAsync(u => u.User_ID == (directConversation.User1_ID == currentUser.Id ? directConversation.User2_ID : directConversation.User1_ID));

                    var newMessage = new DirectMessage_DTO
                    {
                        Conversation_ID = conversationID,
                        Sender_ID = currentUser.Id,
                        Sender_Name = user.First_Name + " " + user.Last_Name,
                        Message = message,
                        Timestamp = newDirectMessage.Timestamp
                    };

                    //send message to all clients in the conversation
                    //TODO send message to WEBSOCKET connections is valid

                    return Ok(newMessage);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("Direct/{conversation_ID}/mark-as-read")]
        [Authorize]
        public async Task<IActionResult> MarkDirectConversationAsRead([FromQuery] Guid conversationID)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get direct conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    bool valid = await ValidateUserDirectConversationAccess(conversationID, currentUser.Id);

                    if (!valid)
                    {
                        return Unauthorized("You are not part of this direct conversation");
                    }

                    //get messages for direct conversation
                    var messages = await context.DirectMessages.Where(dm => dm.Conversation_ID == conversationID && dm.Sender_ID != currentUser.Id && dm.Seen == false).ToListAsync();

                    foreach (var message in messages)
                    {
                        message.Seen = true;
                    }

                    await context.SaveChangesAsync();

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("Group/{conversationID}/messages")]
        [Authorize]
        public async Task<IActionResult> GetMessagesForGroupMessageThread([FromQuery] Guid conversationID, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get group conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    //check if user is part of group conversation
                    var validAccess = await ValidateUserGroupConversationAccess(conversationID, currentUser.Id);

                    if (!validAccess)
                    {
                        return Unauthorized("You are not part of this group conversation");
                    }

                    //get group conversation
                    var groupConversation = await context.GroupConversations.FirstOrDefaultAsync(gc => gc.Conversation_ID == conversationID);

                    //get messages for group conversation
                    var messages = await context.GroupConversationMessages.Where(gcm => gcm.Conversation_ID == conversationID).OrderByDescending(gcm => gcm.Timestamp).ToListAsync();

                    //convert messages to DTO
                    var messages_DTO = new List<GroupMessage_DTO>();

                    foreach (var message in messages)
                    {
                        var sender = await context.Users.FirstOrDefaultAsync(u => u.User_ID == message.Sender_ID);

                        messages_DTO.Add(new GroupMessage_DTO
                        {
                            Conversation_ID = message.Conversation_ID,
                            Sender_ID = message.Sender_ID,
                            Sender_Name = sender.First_Name + " " + sender.Last_Name,
                            Message = message.Message,
                            Timestamp = message.Timestamp
                        });
                    }

                    return Ok(messages_DTO);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        [HttpPost("Group/{conversationID}/send-message")]
        [Authorize]
        public async Task<IActionResult> SendMessageToGroupThread([FromQuery] Guid conversationID, [FromBody] string message)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get group conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    //check if user is part of group conversation
                    var validAccess = await ValidateUserGroupConversationAccess(conversationID, currentUser.Id);

                    if (!validAccess)
                    {
                        return Unauthorized("You are not part of this group conversation");
                    }

                    //get group conversation
                    var groupConversation = await context.GroupConversations.FirstOrDefaultAsync(gc => gc.Conversation_ID == conversationID);

                    //create new group message
                    var newGroupMessage = new GroupConversationMessage
                    {
                        Conversation_ID = conversationID,
                        Sender_ID = currentUser.Id,
                        Message = message,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    //find and update GroupConversationUserSeenStatus
                    //get all users in group conversation
                    var groupConversationUsers = await context.GroupConversationUserSeenStatuses.Where(gcuss => gcuss.GroupConversation_ID == conversationID).ToListAsync();

                    //remove current user from list
                    groupConversationUsers.RemoveAll(cu => cu.User_ID == currentUser.Id);


                    //update seen status
                    groupConversationUsers.ForEach(gcuss => gcuss.Seen = false);

                    //save new group message
                    await context.GroupConversationMessages.AddAsync(newGroupMessage);
                    await context.SaveChangesAsync();

                    var sender = await context.Users.FirstOrDefaultAsync(u => u.User_ID == currentUser.Id);

                    var newMessage = new GroupMessage_DTO
                    {
                        Conversation_ID = conversationID,
                        Sender_ID = currentUser.Id,
                        Sender_Name = sender.First_Name + " " + sender.Last_Name,
                        Message = message,
                        Timestamp = newGroupMessage.Timestamp
                    };

                    //send message to all clients in the conversation
                    //TODO send message to WEBSOCKET connections

                    return Ok(newMessage);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("Group/{conversation_ID}/mark-as-seen")]
        [Authorize]
        public async Task<IActionResult> MarkGroupConversationAsSeen([FromQuery] Guid conversationID)
        {
            try
            {
                //get current user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized("User was not found");
                }

                //get group conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    //check if user is part of group conversation
                    var validAccess = await ValidateUserGroupConversationAccess(conversationID, currentUser.Id);

                    if (!validAccess)
                    {
                        return Unauthorized("You are not part of this group conversation");
                    }

                    //get GroupConversationUserSeenStatus
                    var groupConversationUserSeenStatus = await context.GroupConversationUserSeenStatuses.FirstOrDefaultAsync(gcuss => gcuss.GroupConversation_ID == conversationID && gcuss.User_ID == currentUser.Id);

                    if (groupConversationUserSeenStatus == null)
                    {
                        return Ok("There was an error marking the group conversation as seen");
                    }

                    //update seen status
                    groupConversationUserSeenStatus.Seen = true;

                    //save changes
                    await context.SaveChangesAsync();

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }




    }
}

