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
                    usersInTeam.Remove(currentUserEntity);

                    if(usersInTeam == null)
                    {
                        return Ok("");
                    }

                    if(usersInTeam.Count == 0)
                    {
                        return Ok("");
                    }

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
                            dCDTO.LastMessageTimestamp = lastMessage.Timestamp;
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
        [HttpPost("new-direct-message-thread")]
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

                //create new direct conversation
                var newDC_Entity = new DirectConversation
                {
                    Conversation_ID = Guid.NewGuid(),
                    User1_ID = currentUser.Id,
                    User2_ID = user2.Id,
                    Date_Created = DateTimeOffset.UtcNow
                };

                //save new direct conversation
                using (var context = _contextFactory.CreateDbContext())
                {
                    //ensure there is not already a direct conversation between these two users
                    var existingDC = await context.DirectConversations.FirstOrDefaultAsync(dc => dc.User1_ID == currentUser.Id && dc.User2_ID == user2.Id);

                    if (existingDC != null)
                    {
                        return BadRequest("A direct conversation already exists between these two users");
                    }

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
                    var messages = await context.DirectMessages.Where(dm => dm.Conversation_ID == conversationID).OrderByDescending(dm => dm.Timestamp).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
                    var otherUser = await context.Users.FirstOrDefaultAsync(u => u.User_ID == (directConversation.User1_ID == currentUser.Id ? directConversation.User2_ID : directConversation.User1_ID));

                    //convert messages to DTO
                    var messages_DTO = new List<DirectMessage_DTO>();

                    foreach (var message in messages)
                    {
                        messages_DTO.Add(new DirectMessage_DTO
                        {
                            Sender_ID = message.Sender_ID,
                            Sender_Name = otherUser.First_Name + " " + otherUser.Last_Name,
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


        //send message to a thread
        [HttpPost("Direct/{conversationID}/send-message")]
        [Authorize]
        public async Task<IActionResult> SendMessageToDirectThread([FromQuery] Guid conversationID, [FromBody] DirectMessage_DTO message)
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
                        Message = message.Message,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    //save new direct message
                    await context.DirectMessages.AddAsync(newDirectMessage);
                    await context.SaveChangesAsync();

                    //send message to all clients in the conversation
                    //TODO send message to WEBSOCKET connections is valid

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
