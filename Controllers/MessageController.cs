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

        private async Task<bool> ValidateUserConversationAccess(Guid conversationId, string userId)
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

                    if(currentUserEntity == null)
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
                    //get direct conversations
                    var directConversations = await context.DirectConversations.Where(dc => dc.User1_ID == currentUser.Id || dc.User2_ID == currentUser.Id).ToListAsync();

                    //get group conversations
                    var groupConversations = await context.ConversationUsers.Where(cu => cu.User_ID == currentUser.Id).ToListAsync();

                    //convert direct conversations to DTO
                    var directConversations_DTO = new List<DirectConversation_DTO>();

                    foreach (var conversation in directConversations)
                    {
                        DirectConversation_DTO dCDTO = new DirectConversation_DTO();
                        dCDTO.Conversation_ID = conversation.Conversation_ID;
                        dCDTO.Conversation_Type = "DIRECT";

                        //get other user id. Where not equal to current user id
                        string otherUserId = conversation.User1_ID == currentUser.Id ? conversation.User2_ID : conversation.User1_ID;

                        dCDTO.User2_ID = otherUserId;

                        //get other user
                        var otherUser = await context.Users.FirstOrDefaultAsync(u => u.User_ID == otherUserId);


                        if (otherUser == null)
                        {
                            dCDTO.User2_Name = "UNKNOWN";
                        }
                        else
                        {
                            dCDTO.User2_Name = otherUser.First_Name + " " + otherUser.Last_Name;
                        }

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

                        directConversations_DTO.Add(dCDTO);

                    }

                    //convert group conversations to DTO //TODO

                    return Ok(directConversations_DTO);
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
                    using (var transaction = context.Database.BeginTransaction()) //using transaction to ensure all changes are rolled back if an error occurs. Incase a group conversation is created but not all users are added
                    {
                        try
                        {
                            var creator = await context.Users.FirstOrDefaultAsync(u => u.User_ID == currentUser.Id);
                            if (creator == null)
                            {
                                return Unauthorized("User was not found");
                            }

                            if (newGC_DTO.GroupName == null || newGC_DTO.GroupName == "")
                            {
                                return BadRequest("Group name was not provided");
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

                            List<ConversationUsers> conversationUsers = new List<ConversationUsers>();
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

                                conversationUsers.Add(new ConversationUsers
                                {
                                    Conversation_ID = newGroupConversation.Conversation_ID,
                                    User_ID = userID
                                });
                            }

                            await context.ConversationUsers.AddRangeAsync(conversationUsers);
                            await context.SaveChangesAsync(); 
                            await transaction.CommitAsync(); //important to commit changes to database

                            GroupConversation_DTO groupConversation_DTO = new GroupConversation_DTO
                            {
                                Conversation_ID = newGroupConversation.Conversation_ID,
                                Conversation_Type = 1,
                                Creator_Name = creator.First_Name + " " + creator.Last_Name,
                                GroupName = newGroupConversation.Title,
                                LastMessageTimestamp = null,
                                hasUnreadMessages = false
                            };

                            return Ok(groupConversation_DTO);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync(); //important to rollback if exception occurs and remove database clutter
                            return BadRequest(ex.Message);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }   

        //get my message threads

        //get messages for a direct conversation
        [HttpGet("Direct/{conversationID}/messages")]
        [Authorize]
        public async Task<IActionResult> GetMessagesForDirectMessageThread([FromQuery]Guid conversationID, [FromQuery]int pageIndex = 1, [FromQuery] int pageSize = 20)
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

                    bool valid = await ValidateUserConversationAccess(conversationID, currentUser.Id);

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
                    bool valid = await ValidateUserConversationAccess(conversationID, currentUser.Id);

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
                    bool valid = await ValidateUserConversationAccess(conversationID, currentUser.Id);

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
    }
}
